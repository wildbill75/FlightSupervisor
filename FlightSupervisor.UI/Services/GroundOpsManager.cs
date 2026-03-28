using System;
using System.Collections.Generic;
using System.Linq;
using FlightSupervisor.UI.Models.SimBrief;

namespace FlightSupervisor.UI.Services
{
    public enum GroundServiceState { NotStarted, InProgress, Delayed, Completed, Skipped, WaitingForAction }

    public enum GroundOpsSpeed { Realistic, Short, Instant }

    public class GroundService
    {
        public string Name { get; set; } = "";
        public GroundServiceState State { get; set; } = GroundServiceState.NotStarted;
        public int TotalDurationSec { get; set; }
        public int ElapsedSec { get; set; }
        public int DelayAddedSec { get; set; }
        public string StatusMessage { get; set; } = "Pending";
        public string ActiveDelayEvent { get; set; } = "";
        public bool IsOptional { get; set; }
        public bool HasBeenDelayed { get; set; } = false;
        
        // Story 29: Scheduled Ground Services
        public int StartOffsetMinutes { get; set; }
        public bool RequiresManualStart { get; set; }

        public int RemainingSec => Math.Max(0, (TotalDurationSec + DelayAddedSec) - ElapsedSec);
        public int ProgressPercent => (TotalDurationSec + DelayAddedSec) == 0 ? 100 : (int)Math.Min(100, Math.Max(0, ((double)ElapsedSec / (TotalDurationSec + DelayAddedSec)) * 100));
    }

    public class DelayEvent
    {
        public string DescriptionEn { get; set; } = "";
        public string DescriptionFr { get; set; } = "";
        public string Description => LocalizationService.Translate(DescriptionEn, DescriptionFr);
        public int MinDelaySec { get; set; }
        public int MaxDelaySec { get; set; }
    }

    public class GroundOpsManager
    {
        public List<GroundService> Services { get; private set; } = new();
        public GroundOpsSpeed SpeedSetting { get; set; } = GroundOpsSpeed.Realistic;
        public int EventProbabilityPercent { get; set; } = 20;
        private Random _rnd = new Random();
        private bool _isStarted = false;
        private DateTime _lastTick;
        // Story 29
        public DateTime? TargetSobt { get; private set; }

        private static readonly Dictionary<string, List<DelayEvent>> _delayEvents = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Refueling", new List<DelayEvent> {
                new() { DescriptionEn = "Gauge malfunction", DescriptionFr = "Problème de jauge", MinDelaySec = 120, MaxDelaySec = 300 },
                new() { DescriptionEn = "Fuel truck delayed", DescriptionFr = "Camion en retard", MinDelaySec = 180, MaxDelaySec = 480 },
                new() { DescriptionEn = "Spill safety procedure", DescriptionFr = "Procédure anti-déversement", MinDelaySec = 300, MaxDelaySec = 600 }
            }},
            { "Boarding", new List<DelayEvent> {
                new() { DescriptionEn = "Missing passenger", DescriptionFr = "Passager manquant", MinDelaySec = 300, MaxDelaySec = 900 },
                new() { DescriptionEn = "Waiting for PRM assistance", DescriptionFr = "Attente assistance PMR", MinDelaySec = 180, MaxDelaySec = 420 },
                new() { DescriptionEn = "Jetbridge fault", DescriptionFr = "Problème passerelle", MinDelaySec = 240, MaxDelaySec = 600 },
                new() { DescriptionEn = "Offloading baggage", DescriptionFr = "Débarquement bagage", MinDelaySec = 300, MaxDelaySec = 480 }
            }},
            { "Cargo", new List<DelayEvent> {
                new() { DescriptionEn = "Oversized freight", DescriptionFr = "Fret hors dimension", MinDelaySec = 300, MaxDelaySec = 720 },
                new() { DescriptionEn = "Loader breakdown", DescriptionFr = "Panne du chargeur", MinDelaySec = 300, MaxDelaySec = 600 },
                new() { DescriptionEn = "Late connecting bags", DescriptionFr = "Retard bagages correspondance", MinDelaySec = 180, MaxDelaySec = 480 }
            }},
            { "Catering", new List<DelayEvent> {
                new() { DescriptionEn = "Missing meal carts", DescriptionFr = "Chariots repas manquants", MinDelaySec = 300, MaxDelaySec = 600 },
                new() { DescriptionEn = "Security check", DescriptionFr = "Contrôle sûreté chariot", MinDelaySec = 180, MaxDelaySec = 360 }
            }},
            { "Cleaning", new List<DelayEvent> {
                new() { DescriptionEn = "Deep cleaning required", DescriptionFr = "Nettoyage approfondi", MinDelaySec = 180, MaxDelaySec = 420 },
                new() { DescriptionEn = "Staff shortage", DescriptionFr = "Manque de personnel", MinDelaySec = 240, MaxDelaySec = 480 }
            }},
            { "PNC Chores", new List<DelayEvent> {
                new() { DescriptionEn = "Messy cabin", DescriptionFr = "Cabine très sale", MinDelaySec = 120, MaxDelaySec = 300 },
                new() { DescriptionEn = "Missing supplies", DescriptionFr = "Manque de matériel", MinDelaySec = 180, MaxDelaySec = 360 }
            }},
            { "Water/Waste", new List<DelayEvent> {
                new() { DescriptionEn = "Hose connection leak", DescriptionFr = "Fuite tuyau raccordement", MinDelaySec = 120, MaxDelaySec = 360 },
                new() { DescriptionEn = "Vehicle unavailable", DescriptionFr = "Véhicule indisponible", MinDelaySec = 300, MaxDelaySec = 720 }
            }}
        };

        public event Action? OnOpsCompleted;
        public event Action? OnOpsUpdated;
        public event Action<string>? OnOpsLog;

        public void InitializeFromSimBrief(SimBriefResponse? sb)
        {
            Services.Clear();
            TargetSobt = null;

            int pax = 0;
            int.TryParse(sb?.Weights?.PaxCount, out pax);
            int fuel = 0;
            int.TryParse(sb?.Fuel?.PlanRamp, out fuel);

            bool isHeavy = false;
            if (sb != null)
            {
                if (sb.Times?.SchedOut != null && long.TryParse(sb.Times.SchedOut, out long unixSobt))
                {
                    TargetSobt = DateTimeOffset.FromUnixTimeSeconds(unixSobt).UtcDateTime;
                }

                string acType = sb.Aircraft?.BaseType ?? sb.Aircraft?.IcaoCode ?? "";
                if (acType.StartsWith("A33") || acType.StartsWith("A34") || acType.StartsWith("A35") || acType.StartsWith("A38") || 
                    acType.StartsWith("B74") || acType.StartsWith("B76") || acType.StartsWith("B77") || acType.StartsWith("B78") || acType.StartsWith("MD1"))
                {
                    isHeavy = true;
                }
            }

            double multiplier = SpeedSetting switch {
                GroundOpsSpeed.Instant => 0.05,
                GroundOpsSpeed.Short => 0.4,
                _ => 1.0
            };

            int applyTime(double baseSec)
            {
                double variation = 1.0 + (_rnd.NextDouble() * 0.30 - 0.15); // +/- 15%
                return (int)Math.Max(2.0, baseSec * variation * multiplier);
            }

            int boardOffset = isHeavy ? -50 : -40;
            int cargoOffset = isHeavy ? -60 : -45;
            int caterOffset = isHeavy ? -60 : -45;
            int cleanOffset = isHeavy ? -65 : -50;
            int waterOffset = isHeavy ? -65 : -50;

            string cleanName = "Cleaning";
            if (sb != null)
            {
                string airline = sb.General?.Airline?.ToUpper() ?? "";
                string iata = sb.General?.IataAirline?.ToUpper() ?? "";
                string[] lowCosts = { "RYR", "EZY", "EJU", "EZS", "WZZ", "VOE", "VLG", "SWA", "NKS", "JBU", "AWE" };
                string[] lowCostIata = { "FR", "U2", "EC", "DS", "W6", "V7", "VY", "WN", "NK", "B6" };
                
                if (lowCosts.Contains(airline) || lowCostIata.Contains(iata))
                {
                    cleanName = "PNC Chores";
                }
            }

            Services.Add(new GroundService { Name = "Refueling", TotalDurationSec = applyTime(Math.Max(60, fuel / 50)), IsOptional = false, RequiresManualStart = true, StartOffsetMinutes = 0 });
            Services.Add(new GroundService { Name = "Boarding", TotalDurationSec = applyTime(Math.Max(120, pax * 6)), IsOptional = false, StartOffsetMinutes = boardOffset });
            Services.Add(new GroundService { Name = "Cargo", TotalDurationSec = applyTime(Math.Max(120, pax * 5)), IsOptional = false, StartOffsetMinutes = cargoOffset });
            Services.Add(new GroundService { Name = "Catering", TotalDurationSec = applyTime(180), IsOptional = true, StartOffsetMinutes = caterOffset });
            Services.Add(new GroundService { Name = cleanName, TotalDurationSec = applyTime(300), IsOptional = true, StartOffsetMinutes = cleanOffset });
            Services.Add(new GroundService { Name = "Water/Waste", TotalDurationSec = applyTime(150), IsOptional = true, StartOffsetMinutes = waterOffset });
            
            _isStarted = false;
            IsPaused = false;
        }

        public bool IsPaused { get; set; } = false;

        public void StartOps()
        {
            if (_isStarted) return;
            _isStarted = true;
            _lastTick = DateTime.UtcNow;
            // No longer forcing to InProgress here. Tick() will sort them out based on time.
        }
        
        // Manual trigger capability for Refueling or other paused features
        public void StartManualService(string name)
        {
            var s = Services.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (s != null && s.State == GroundServiceState.WaitingForAction)
            {
                s.State = GroundServiceState.InProgress;
                s.StatusMessage = LocalizationService.Translate("In Progress", "En cours");
                s.RequiresManualStart = false; // consume the block
                OnOpsLog?.Invoke(LocalizationService.Translate($"[RAMP AGENT] {name} starting manually.", $"[RAMP AGENT] L'opération {name} demandée manuellement a démarré."));
                OnOpsUpdated?.Invoke();
            }
        }

        public void SkipService(string name)
        {
            var s = Services.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (s != null && s.IsOptional && s.State != GroundServiceState.Completed)
            {
                s.State = GroundServiceState.Skipped;
                s.StatusMessage = LocalizationService.Translate("Skipped by Capt.", "Ignoré par le Cdt.");
                s.ElapsedSec = s.TotalDurationSec + s.DelayAddedSec;
                OnOpsUpdated?.Invoke();
            }
        }

        // New warp button
        public void ForceCompleteAllServices()
        {
            if (!_isStarted) return;
            foreach (var s in Services)
            {
                if (s.State != GroundServiceState.Completed && s.State != GroundServiceState.Skipped)
                {
                    s.State = GroundServiceState.Completed;
                    s.StatusMessage = LocalizationService.Translate("Completed", "Terminé");
                    s.ElapsedSec = s.TotalDurationSec + s.DelayAddedSec;
                }
            }
            OnOpsLog?.Invoke(LocalizationService.Translate("[CAPTAIN] Time Warp initiated. All ground operations successfully rushed to completion.", "[CAPTAIN] Time Warp declenché. Toutes les opérations terminées instantanément."));
            OnOpsUpdated?.Invoke();
            OnOpsCompleted?.Invoke();
            _isStarted = false;
        }

        // Overloaded Tick to receive Current MSFS Zulu Time from MainWindow
        public void Tick(DateTime? currentZulu)
        {
            if (!_isStarted || IsPaused) return;
            var now = DateTime.UtcNow;
            var delta = (int)(now - _lastTick).TotalSeconds;
            if (delta <= 0) return;
            _lastTick = now;

            bool allDone = true;
            bool changed = false;

            DateTime simTime = currentZulu ?? DateTime.UtcNow;
            if (simTime.Year < 2000) simTime = DateTime.UtcNow;

            foreach (var s in Services)
            {
                if (s.State == GroundServiceState.Completed || s.State == GroundServiceState.Skipped) continue;
                allDone = false;

                // Time-gated starts
                if (s.State == GroundServiceState.NotStarted)
                {
                    if (s.RequiresManualStart)
                    {
                        s.State = GroundServiceState.WaitingForAction;
                        s.StatusMessage = LocalizationService.Translate("Waiting for Pilot...", "En attente d'action Cdt...");
                        changed = true;
                        continue;
                    }

                    bool shouldStart = true;
                    if (TargetSobt != null)
                    {
                        var startZulu = TargetSobt.Value.AddMinutes(s.StartOffsetMinutes);
                        if (simTime < startZulu)
                        {
                            var waitSec = (int)(startZulu - simTime).TotalSeconds;
                            s.StatusMessage = LocalizationService.Translate($"Scheduled in {waitSec/60}m", $"Prévu dans {waitSec/60}m");
                            shouldStart = false;
                        }
                    }

                    if (shouldStart)
                    {
                        var boarding = Services.FirstOrDefault(x => x.Name == "Boarding");
                        var catering = Services.FirstOrDefault(x => x.Name == "Catering");

                        if (s.Name == "Boarding" && catering != null && (catering.State == GroundServiceState.InProgress || catering.State == GroundServiceState.Delayed))
                        {
                            shouldStart = false;
                            s.StatusMessage = LocalizationService.Translate("Waiting for Catering", "Attente Catering");
                        }
                        else if ((s.Name == "Cleaning" || s.Name == "PNC Chores" || s.Name == "Catering") && boarding != null && boarding.State != GroundServiceState.NotStarted && boarding.State != GroundServiceState.Skipped)
                        {
                            // If boarding has started or finished, cleaning and catering can't happen anymore.
                            s.State = GroundServiceState.Skipped;
                            s.StatusMessage = LocalizationService.Translate("Skipped (Pax on board)", "Annulé (Pax à bord)");
                            changed = true;
                            string actor = GetActorForService(s.Name);
                            OnOpsLog?.Invoke(LocalizationService.Translate(
                                $"[{actor}] {s.Name} skipped because passengers are already boarding or onboard.", 
                                $"[{actor}] {s.Name} annulé car les passagers embarquent ou sont à bord."));
                            continue;
                        }
                    }

                    if (shouldStart)
                    {
                        s.State = GroundServiceState.InProgress;
                        s.StatusMessage = LocalizationService.Translate("In Progress", "En cours");
                        changed = true;
                        
                        // Virtual Actors Logging Start Events
                        string actor = GetActorForService(s.Name);
                        string startMsg = GetStartMessageForService(s.Name);
                        OnOpsLog?.Invoke(LocalizationService.Translate($"[{actor}] {startMsg}", $"[{actor}] {startMsg}"));
                    }
                    else
                    {
                        continue; // Don't tick progress if not started
                    }
                }

                if (s.State == GroundServiceState.WaitingForAction) continue;

                // Progress the service
                s.ElapsedSec += delta;

                if (s.ElapsedSec >= s.TotalDurationSec + s.DelayAddedSec)
                {
                    s.State = GroundServiceState.Completed;
                    s.StatusMessage = LocalizationService.Translate("Completed", "Terminé");
                    changed = true;
                    
                    string actor = GetActorForService(s.Name);
                    string endMsg = GetEndMessageForService(s.Name);
                    OnOpsLog?.Invoke(LocalizationService.Translate($"[{actor}] {endMsg}", $"[{actor}] {endMsg}"));
                }
                else
                {
                    double chance = EventProbabilityPercent * 0.000015;
                    if (!s.HasBeenDelayed && s.State != GroundServiceState.Delayed && chance > 0 && _rnd.NextDouble() < chance) 
                    {
                        s.State = GroundServiceState.Delayed;
                        s.HasBeenDelayed = true;
                        
                        string eventDesc = "Perturbation inopinée";
                        int additionalDelay = _rnd.Next(60, 180);
                        
                        if (_delayEvents.TryGetValue(s.Name, out var eventsList) && eventsList.Count > 0)
                        {
                            var evt = eventsList[_rnd.Next(eventsList.Count)];
                            eventDesc = evt.Description;
                            additionalDelay = _rnd.Next(evt.MinDelaySec, evt.MaxDelaySec);
                        }

                        s.ActiveDelayEvent = eventDesc;
                        s.DelayAddedSec += additionalDelay;
                        s.StatusMessage = LocalizationService.Translate($"Delay: {eventDesc} (+{(additionalDelay/60)}m)", $"Retard: {eventDesc} (+{(additionalDelay/60)}m)");
                        changed = true;
                        
                        string actor = GetActorForService(s.Name);
                        OnOpsLog?.Invoke(LocalizationService.Translate($"[{actor}] Ground Op issue: {eventDesc} (+{(additionalDelay/60)} min)", $"[{actor}] Problème sur l'escale : {eventDesc} (+{(additionalDelay/60)} min)"));
                    }
                    // Recover from delay if progressing normally again
                    else if (s.State == GroundServiceState.Delayed && _rnd.NextDouble() < 0.05)
                    {
                        s.State = GroundServiceState.InProgress;
                        s.ActiveDelayEvent = "";
                        s.StatusMessage = LocalizationService.Translate("In Progress", "En cours");
                        changed = true;
                        string actor = GetActorForService(s.Name);
                        OnOpsLog?.Invoke(LocalizationService.Translate($"[{actor}] Issue resolved. Operations resumed.", $"[{actor}] Problème résolu. Reprise de l'opération."));
                    }
                }
            }

            if (changed) OnOpsUpdated?.Invoke();
            if (allDone) { _isStarted = false; OnOpsCompleted?.Invoke(); }
        }
        
        // --- Virtual Actor Data Mapping ---
        private string GetActorForService(string name)
        {
            switch (name.ToLower())
            {
                case "board":
                case "boarding":
                    return "GATE AGENT";
                case "catering":
                case "cleaning":
                case "pnc chores":
                    return "PURSER";
                default: // Fuel, Cargo, Water
                    return "RAMP AGENT";
            }
        }

        private string GetStartMessageForService(string name)
        {
            switch (name.ToLower())
            {
                case "boarding": return "We are starting general boarding at the terminal.";
                case "catering": return "Loading the galley catering carts.";
                case "cleaning": return "Cleaning crew has entered the cabin.";
                case "pnc chores": return "Cabin crew is preparing the cabin for the next flight.";
                case "cargo": return "Starting lower deck cargo loading.";
                case "water/waste": return "Servicing the blue water logic systems.";
                default: return $"{name} is underway.";
            }
        }

        private string GetEndMessageForService(string name)
        {
            switch (name.ToLower())
            {
                case "boarding": return "Passenger count verified. Cabin is secured.";
                case "catering": return "All trolleys are locked. Catering is completed.";
                case "cleaning": return "Cabin interior is tidy and ready for flight.";
                case "pnc chores": return "Cabin is secured by the crew for the next flight.";
                case "cargo": return "Holds are closed and weight is verified.";
                case "water/waste": return "Service trucks are driving away.";
                case "refueling": return "Hoses disconnected. Slip is signed.";
                default: return $"{name} completed.";
            }
        }

        // Maintain old overload for compatibility during compile transitions
        public void Tick()
        {
            Tick(null);
        }

        public string GetStatusString()
        {
            if (Services.Count == 0) return "Ground Ops: En attente d'Initialisation";
            var lines = Services.Select(s => $"{s.Name.PadRight(12)}: [{s.ProgressPercent,3}%] {s.StatusMessage.PadRight(20)} {- s.RemainingSec}s");
            return string.Join("\n", lines);
        }

        public bool IsAnyOperationInProgress()
        {
            return _isStarted && Services.Any(s => s.State == GroundServiceState.InProgress || s.State == GroundServiceState.Delayed || s.State == GroundServiceState.NotStarted);
        }

        public void AbortAllOperations()
        {
            if (!_isStarted) return;
            foreach (var s in Services)
            {
                if (s.State != GroundServiceState.Completed && s.State != GroundServiceState.Skipped)
                {
                    s.State = GroundServiceState.Skipped; // Reusing skipped logic internally, but modifying text
                    s.StatusMessage = LocalizationService.Translate("ABORTED!", "ANNULÉ !");
                    s.ElapsedSec = s.TotalDurationSec + s.DelayAddedSec;
                }
            }
            _isStarted = false;
            OnOpsUpdated?.Invoke();
        }
    }
}

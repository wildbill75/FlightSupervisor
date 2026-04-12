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
        public bool IsPreServiced { get; set; } = false;
        
        // Story 29: Scheduled Ground Services
        public int StartOffsetMinutes { get; set; }
        // Story 42/43: Visual Inhibition & Dependencies
        public bool IsAvailable { get; set; } = true;

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
        public double CurrentCrewEfficiency { get; set; } = 100.0;
        public int EventProbabilityPercent { get; set; } = 20;
        public string CurrentAirportTier { get; private set; } = "Tier A";
        public string CurrentAirportTierDescription { get; private set; } = "Most of the time, this airport has excellent infrastructure and operations are quick.";
        private Random _rnd = new Random();
        private bool _isStarted = false;
        private DateTime _lastTick;
        // Story 29
        // Story 29
        public DateTime? TargetSobt { get; private set; }
        public bool IsLowCost { get; private set; } = false;
        
        // Turnaround Phase Context
        public FlightPhase CurrentPhase { get; set; } = FlightPhase.AtGate;
        public bool IsSeatbeltsOn { get; set; } = true;
        public bool IsBeaconOn { get; set; } = false;
        public double EngineMaxN1 { get; set; } = 0.0;
        private bool _hasEmittedN1Warning = false;

        public bool IsFuelSheetValidated { get; set; } = false;

        private static readonly Dictionary<string, List<DelayEvent>> _delayEvents = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Refueling", new List<DelayEvent> {
                new() { DescriptionEn = "Gauge malfunction", DescriptionFr = "Problème de jauge", MinDelaySec = 60, MaxDelaySec = 120 },
                new() { DescriptionEn = "Fuel truck delayed", DescriptionFr = "Camion en retard", MinDelaySec = 90, MaxDelaySec = 180 },
                new() { DescriptionEn = "Spill safety procedure", DescriptionFr = "Procédure anti-déversement", MinDelaySec = 120, MaxDelaySec = 240 }
            }},
            { "Boarding", new List<DelayEvent> {
                new() { DescriptionEn = "Missing passenger", DescriptionFr = "Passager manquant", MinDelaySec = 120, MaxDelaySec = 300 },
                new() { DescriptionEn = "Waiting for PRM assistance", DescriptionFr = "Attente assistance PMR", MinDelaySec = 60, MaxDelaySec = 180 },
                new() { DescriptionEn = "Jetbridge fault", DescriptionFr = "Problème passerelle", MinDelaySec = 90, MaxDelaySec = 240 },
                new() { DescriptionEn = "Offloading baggage", DescriptionFr = "Débarquement bagage", MinDelaySec = 120, MaxDelaySec = 240 }
            }},
            { "Cargo", new List<DelayEvent> {
                new() { DescriptionEn = "Oversized freight", DescriptionFr = "Fret hors dimension", MinDelaySec = 120, MaxDelaySec = 300 },
                new() { DescriptionEn = "Loader breakdown", DescriptionFr = "Panne du chargeur", MinDelaySec = 120, MaxDelaySec = 240 },
                new() { DescriptionEn = "Late connecting bags", DescriptionFr = "Retard bagages correspondance", MinDelaySec = 90, MaxDelaySec = 180 }
            }},
            { "Catering", new List<DelayEvent> {
                new() { DescriptionEn = "Missing meal carts", DescriptionFr = "Chariots repas manquants", MinDelaySec = 120, MaxDelaySec = 240 },
                new() { DescriptionEn = "Security check", DescriptionFr = "Contrôle sûreté chariot", MinDelaySec = 60, MaxDelaySec = 120 }
            }},
            { "Cleaning", new List<DelayEvent> {
                new() { DescriptionEn = "Deep cleaning required", DescriptionFr = "Nettoyage approfondi", MinDelaySec = 60, MaxDelaySec = 180 },
                new() { DescriptionEn = "Staff shortage", DescriptionFr = "Manque de personnel", MinDelaySec = 90, MaxDelaySec = 180 }
            }},
            { "PNC Chores", new List<DelayEvent> {
                new() { DescriptionEn = "Messy cabin", DescriptionFr = "Cabine très sale", MinDelaySec = 60, MaxDelaySec = 120 },
                new() { DescriptionEn = "Missing supplies", DescriptionFr = "Manque de matériel", MinDelaySec = 60, MaxDelaySec = 120 }
            }},
            { "Water/Waste", new List<DelayEvent> {
                new() { DescriptionEn = "Hose connection leak", DescriptionFr = "Fuite tuyau raccordement", MinDelaySec = 60, MaxDelaySec = 120 },
                new() { DescriptionEn = "Vehicle unavailable", DescriptionFr = "Véhicule indisponible", MinDelaySec = 120, MaxDelaySec = 240 }
            }}
        };

        public event Action? OnOpsCompleted;
        public event Action? OnOpsUpdated;
        public event Action<string>? OnServiceStarted;
        public event Action<string>? OnOpsLog;
        public event Action<int, string>? OnOperationBonusTriggered;
        public event Action<int, string>? OnPenaltyTriggered;

        public void InitializeFromSimBrief(SimBriefResponse? sb, bool firstFlightClean = false, double currentFobKg = 0, double initialCleanliness = 100.0, double initialCatering = 100.0, double initialWater = 100.0, double initialWaste = 0.0, DateTime? overrideSobt = null)
        {
            Services.Clear();
            TargetSobt = null;

            int pax = 0;
            int.TryParse(sb?.Weights?.PaxCount, out pax);
            int fuel = 0;
            int.TryParse(sb?.Fuel?.PlanRamp, out fuel);

            bool isHeavy = false;
            if (overrideSobt.HasValue)
            {
                TargetSobt = overrideSobt.Value;
            }
            else if (sb != null && sb.Times?.SchedOut != null && long.TryParse(sb.Times.SchedOut, out long unixSobt))
            {
                TargetSobt = DateTimeOffset.FromUnixTimeSeconds(unixSobt).UtcDateTime;
            }

            if (sb != null)
            {

                string acType = sb.Aircraft?.BaseType ?? sb.Aircraft?.IcaoCode ?? "";
                if (acType.StartsWith("A33") || acType.StartsWith("A34") || acType.StartsWith("A35") || acType.StartsWith("A38") || 
                    acType.StartsWith("B74") || acType.StartsWith("B76") || acType.StartsWith("B77") || acType.StartsWith("B78") || acType.StartsWith("MD1"))
                {
                    isHeavy = true;
                }
            }

            double multiplier = SpeedSetting switch {
                GroundOpsSpeed.Instant => 0.01,   // Approx ~15 sec for a 20min boarding
                GroundOpsSpeed.Short => 0.25,     // x4 max speed
                _ => 1.0
            };



            int boardOffset = isHeavy ? -50 : -40;
            int cargoOffset = isHeavy ? -60 : -45;
            int caterOffset = isHeavy ? -60 : -45;
            int cleanOffset = isHeavy ? -65 : -50;
            int waterOffset = isHeavy ? -65 : -50;

            string cleanName = "Cleaning";
            IsLowCost = false;
            
            if (sb != null)
            {
                string airline = sb.General?.Airline?.ToUpper() ?? "";
                string iata = sb.General?.IataAirline?.ToUpper() ?? "";
                string[] lowCosts = { "RYR", "EZY", "EJU", "EZS", "WZZ", "VOE", "VLG", "SWA", "NKS", "JBU", "AWE" };
                string[] lowCostIata = { "FR", "U2", "EC", "DS", "W6", "V7", "VY", "WN", "NK", "B6" };
                
                if (lowCosts.Contains(airline) || lowCostIata.Contains(iata))
                {
                    IsLowCost = true;
                }
            }

            // --- TICKET 35 : AIRPORT EFFICIENCY TIER ---
            double tierTimeMultiplier = 1.0;
            CurrentAirportTier = "Tier A"; // Default
            EventProbabilityPercent = 20; // Reset default probability base
            string originIcao = sb?.Origin?.IcaoCode?.ToUpper() ?? "";

            string[] tierS = { "EGSS", "EIDW", "EGKK", "KATL", "EGGW", "LOWW" };
            string[] tierB = { "EGLL", "LFPG", "EDDF", "KORD", "KDFW", "LEBL" };
            string[] tierF = { "EHAM", "KEWR", "LIRF", "LFML" };

            if (tierS.Contains(originIcao)) 
            { 
                CurrentAirportTier = "Tier S"; 
                tierTimeMultiplier = 0.85; 
                CurrentAirportTierDescription = "Most of the time, this airport provides excellent infrastructure and operations are smooth & quick."; 
            }
            else if (tierB.Contains(originIcao)) 
            { 
                CurrentAirportTier = "Tier B"; 
                tierTimeMultiplier = 1.00; // Mega hub: Normal speed due to huge logistics means
                EventProbabilityPercent = 40; // Double risk of random events
                CurrentAirportTierDescription = "A mega hub with immense resources, but extremely high traffic. Base ops are fast, but random delays are common."; 
            }
            else if (tierF.Contains(originIcao)) 
            { 
                CurrentAirportTier = "Tier F"; 
                tierTimeMultiplier = 1.30; 
                EventProbabilityPercent = 50;
                CurrentAirportTierDescription = "This airport has extremely poor infrastructure or congestion. Expect chaotic and long ground operations."; 
            }

            int applyTime(double baseSec)
            {
                double variation = 1.0 + (_rnd.NextDouble() * 0.30 - 0.15); // +/- 15%
                int calculated = (int)Math.Max(2.0, baseSec * variation * multiplier * tierTimeMultiplier);
                
                // Enforce Instant constraint
                if (SpeedSetting == GroundOpsSpeed.Instant && calculated > 15) return 15;
                return calculated;
            }

            // --- TICKET 34 : REALISTIC DURATIONS (Base A320/B737) ---
            // Realistic Narrowbody turnaround:
            // LCC: Deboarding(600s), Clean(300s), Cater(300s/Optional), Fuel(600s), Boarding(900s) = ~30m min
            // Legacy: Deboarding(900s), Clean(900s), Cater(900s), Fuel(600s), Boarding(1200s) = ~45m min
            
            if (IsLowCost) cleanName = "PNC Chores";

            // TICKET 34 & 38 : REALISTIC DURATIONS SCALED BY METRICS
            int deboardingBase = IsLowCost ? 1500 : 2700; // 25m for LCC, 45m for Legacy
            double boardingEfficiencyRatio = Math.Max(50.0, CurrentCrewEfficiency) / 100.0;
            int boardingBase = (int)((IsLowCost ? 900 : 1200) / boardingEfficiencyRatio);
            
            // Cleanliness scale: if 90% clean, only 10% of time needed.
            double dirtyRatio = Math.Max(0.01, 1.0 - (initialCleanliness / 100.0));
            int cleaningBase = IsLowCost ? (int)((400 / boardingEfficiencyRatio) * dirtyRatio) : (int)(900 * dirtyRatio);

            // Catering scale: if 90% full, only 10% time needed.
            double caterRatio = Math.Max(0.01, 1.0 - (initialCatering / 100.0));
            int cateringBase = (int)((IsLowCost ? 300 : 900) * caterRatio);
            
            // Water/Waste scale: time depends on max of water to fill or waste to drain
            double wwRatio = Math.Max(1.0 - (initialWater / 100.0), initialWaste / 100.0);
            wwRatio = Math.Max(0.01, wwRatio);
            int wwBase = (int)(450 * wwRatio);

            // Calculate fuel difference. 1 kg/L roughly. PlanRamp is kg. 
            // We assume refueling is 50kg/sec.
            double fuelNeededKg = Math.Max(0, fuel - currentFobKg);
            int fuelBase = Math.Max(600, (int)(fuelNeededKg / 50.0)); // Minimum 10 minutes ou 50kg/sec

            Services.Add(new GroundService { Name = "Refueling", TotalDurationSec = applyTime(fuelBase), IsOptional = false, RequiresManualStart = true, StartOffsetMinutes = 0 });
            Services.Add(new GroundService { Name = "Boarding", TotalDurationSec = applyTime(boardingBase), IsOptional = false, RequiresManualStart = true, StartOffsetMinutes = boardOffset });
            Services.Add(new GroundService { Name = "Cargo/Luggage", TotalDurationSec = applyTime(Math.Max(600, pax * 6)), IsOptional = false, RequiresManualStart = true, StartOffsetMinutes = cargoOffset });
            Services.Add(new GroundService { Name = "Catering", TotalDurationSec = applyTime(cateringBase), IsOptional = true, RequiresManualStart = true, StartOffsetMinutes = caterOffset });
            Services.Add(new GroundService { Name = cleanName, TotalDurationSec = applyTime(cleaningBase), IsOptional = true, RequiresManualStart = true, StartOffsetMinutes = cleanOffset });
            Services.Add(new GroundService { Name = "Water/Waste", TotalDurationSec = applyTime(wwBase), IsOptional = true, RequiresManualStart = true, StartOffsetMinutes = waterOffset });
            
            // Note: Deboarding is NO LONGER added here. It is an Arrival task added by MainWindow when parked.
            
            if (firstFlightClean)
            {
                // Un avion Pristine n'a pas besoin d'être débarqué.
                Services.RemoveAll(x => x.Name == "Deboarding");

                var cleanNames = new[] { "Catering", cleanName, "Water/Waste" };
                foreach (var s in Services.Where(x => cleanNames.Contains(x.Name)))
                {
                    s.State = GroundServiceState.Completed;
                    s.IsPreServiced = true;
                    s.ElapsedSec = s.TotalDurationSec;
                    s.StatusMessage = "Already Serviced";
                }
            }

            _isStarted = false;
            IsPaused = false;
        }

        public void PrepareNextLeg(double currentFobKg, double currentCleanliness = 100.0, double currentCatering = 100.0)
        {
            // Reset state to allow importing a new flight plan seamlessly
            _isStarted = false;
            IsPaused = false;
            IsFuelSheetValidated = false;
            _hasEmittedN1Warning = false;
            TargetSobt = null;
            Services.Clear();
            Services.Add(new GroundService { Name = "Deboarding", TotalDurationSec = 600, StatusMessage = "Pending", State = GroundServiceState.NotStarted, ElapsedSec = 0, RequiresManualStart = true });
            Services.Add(new GroundService { Name = "Cargo/Luggage", TotalDurationSec = 600, StatusMessage = "Pending", State = GroundServiceState.NotStarted, ElapsedSec = 0, RequiresManualStart = true });
            Services.Add(new GroundService { Name = "Cleaning", TotalDurationSec = 600, StatusMessage = "Pending", State = GroundServiceState.NotStarted, ElapsedSec = 0, RequiresManualStart = true });
            Services.Add(new GroundService { Name = "Catering", TotalDurationSec = 600, StatusMessage = "Pending", State = GroundServiceState.NotStarted, ElapsedSec = 0, RequiresManualStart = true, IsOptional = true });
            Services.Add(new GroundService { Name = "Water/Waste", TotalDurationSec = 300, StatusMessage = "Pending", State = GroundServiceState.NotStarted, ElapsedSec = 0, RequiresManualStart = true, IsOptional = true });
            Services.Add(new GroundService { Name = "Refueling", TotalDurationSec = 600, StatusMessage = "Pending", State = GroundServiceState.NotStarted, ElapsedSec = 0, RequiresManualStart = true });
            Services.Add(new GroundService { Name = "Boarding", TotalDurationSec = 900, StatusMessage = "Pending", State = GroundServiceState.NotStarted, ElapsedSec = 0, RequiresManualStart = true });
            OnOpsUpdated?.Invoke();
        }

        public bool IsPaused { get; set; } = false;

        public void StartOps()
        {
            if (_isStarted) return;
            _isStarted = true;
            _lastTick = DateTime.MinValue; // Force sync on first tick to avoid massive jumps from sim offsets
            
            OnOpsUpdated?.Invoke();
        }

        public void TimeSkip(int minutes)
        {
            if (!_isStarted) return;
            int secondsToAdd = minutes * 60;
            foreach (var s in Services)
            {
                if (s.State == GroundServiceState.InProgress || s.State == GroundServiceState.Delayed)
                {
                    s.ElapsedSec += secondsToAdd;
                }
            }
            // The next real MSFS UI tick will evaluate completions correctly
            OnOpsUpdated?.Invoke();
        }

        public void SetGroundSpeedMultiplier(string speedString)
        {
            if (Enum.TryParse<GroundOpsSpeed>(speedString, true, out var spd))
            {
                if (SpeedSetting == spd) return; // No change

                double oldMultiplier = SpeedSetting switch {
                    GroundOpsSpeed.Instant => 0.01,
                    GroundOpsSpeed.Short => 0.25,
                    _ => 1.0
                };

                SpeedSetting = spd;

                double newMultiplier = SpeedSetting switch {
                    GroundOpsSpeed.Instant => 0.01,
                    GroundOpsSpeed.Short => 0.25,
                    _ => 1.0
                };

                double transitionRatio = newMultiplier / oldMultiplier;

                foreach (var s in Services)
                {
                    if (s.State != GroundServiceState.Completed && s.State != GroundServiceState.Skipped)
                    {
                        s.TotalDurationSec = (int)Math.Max(2.0, s.TotalDurationSec * transitionRatio);
                        
                        // Limit Instant to 15 secs max
                        if (SpeedSetting == GroundOpsSpeed.Instant && s.TotalDurationSec > 15)
                        {
                            s.TotalDurationSec = 15;
                        }
                    }
                }
                
                OnOpsUpdated?.Invoke();
            }
        }
        
        // Manual trigger capability for Refueling or other paused features
        public void StartManualService(string name)
        {
            var s = Services.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || 
                                                 (name.Equals("Cleaning", StringComparison.OrdinalIgnoreCase) && x.Name.Contains("Clean", StringComparison.OrdinalIgnoreCase)) ||
                                                 (name.Equals("Cargo", StringComparison.OrdinalIgnoreCase) && x.Name.Contains("Cargo", StringComparison.OrdinalIgnoreCase)));
            if (s != null && (s.State == GroundServiceState.WaitingForAction || s.State == GroundServiceState.NotStarted))
            {
                if (!_isStarted) StartOps();
                
                // Enforce Constraints
                var boarding = Services.FirstOrDefault(x => x.Name == "Boarding");
                var deboarding = Services.FirstOrDefault(x => x.Name == "Deboarding");
                var cleaning = Services.FirstOrDefault(x => x.Name.Contains("Clean"));
                var catering = Services.FirstOrDefault(x => x.Name == "Catering");

                if (CurrentPhase == FlightPhase.Turnaround)
                {
                    if (s.Name != "Deboarding" && s.Name != "Cargo/Luggage")
                    {
                        s.StatusMessage = LocalizationService.Translate("Wait Turnaround", "Attente Turnaround");
                        return;
                    }
                    else if (s.Name == "Deboarding" && IsSeatbeltsOn)
                    {
                        s.StatusMessage = LocalizationService.Translate("Wait Seatbelts", "Attentes Signes");
                        return;
                    }
                    else if (s.Name == "Cargo/Luggage" && IsBeaconOn)
                    {
                        s.StatusMessage = LocalizationService.Translate("Wait Beacon", "Attente Beacon");
                        return;
                    }
                    else if (EngineMaxN1 > 5.0 && !_hasEmittedN1Warning)
                    {
                        _hasEmittedN1Warning = true;
                        OnOpsLog?.Invoke(LocalizationService.Translate("[WARNING] Ground Ops started while engines are running! This is extremely dangerous. Penalty applied.", "[WARNING] Opération lancée moteurs allumés ! Très dangereux. Pénalité affectée."));
                        OnPenaltyTriggered?.Invoke(-50, "Engines running during Turnaround OPs");
                    }
                }

                bool isPaxMoving = (boarding != null && boarding.State == GroundServiceState.InProgress) || 
                                   (deboarding != null && deboarding.State == GroundServiceState.InProgress);

                if ((name.Equals("Cleaning", StringComparison.OrdinalIgnoreCase) || name.Equals("Catering", StringComparison.OrdinalIgnoreCase)) && isPaxMoving)
                {
                    s.StatusMessage = LocalizationService.Translate("Blocked (Pax)", "Bloqué (Pax)");
                    OnOpsLog?.Invoke(LocalizationService.Translate($"[{GetActorForService(s.Name)}] Cannot start {s.Name} while passengers are boarding/deboarding.", $"[{GetActorForService(s.Name)}] Impossible de démarrer {s.Name} pendant le mouvement des passagers."));
                    OnOpsUpdated?.Invoke();
                    return;
                }

                if (name.Equals("Boarding", StringComparison.OrdinalIgnoreCase))
                {
                    bool cleaningActive = cleaning != null && cleaning.State == GroundServiceState.InProgress;
                    bool cateringActive = catering != null && catering.State == GroundServiceState.InProgress;
                    
                    if (cleaningActive || cateringActive)
                    {
                        s.StatusMessage = LocalizationService.Translate("Blocked (Crew)", "Bloqué (Serv.)");
                        OnOpsLog?.Invoke(LocalizationService.Translate($"[{GetActorForService(s.Name)}] Cannot start Boarding while Cleaning or Catering is in progress.", $"[{GetActorForService(s.Name)}] L'embarquement est bloqué car le nettoyage ou le catering est en cours."));
                        OnOpsUpdated?.Invoke();
                        return;
                    }
                }

                if (name.Equals("Refueling", StringComparison.OrdinalIgnoreCase))
                {
                    if (!IsFuelSheetValidated)
                    {
                        s.StatusMessage = LocalizationService.Translate("Awaiting Validation", "Attente Validation");
                        OnOpsLog?.Invoke(LocalizationService.Translate(
                            $"[DISPATCH] Cannot commence Refueling until Fuel Load Sheet is strictly validated by the Commander.", 
                            $"[DISPATCH] Impossible de commencer le ravitaillement, la confirmation en carburant n'a pas été validée par le Cdt."));
                        OnOpsUpdated?.Invoke();
                        return; // BLOCKED
                    }
                }

                s.State = GroundServiceState.InProgress;
                s.StatusMessage = LocalizationService.Translate("In Progress", "En cours");
                s.RequiresManualStart = false; // consume the block
                
                string actor = GetActorForService(s.Name);
                string startMsg = GetStartMessageForService(s.Name);
                OnOpsLog?.Invoke(LocalizationService.Translate($"[{actor}] {startMsg} (Manual Start)", $"[{actor}] {startMsg} (Manuel)"));
                OnServiceStarted?.Invoke(s.Name);
                
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

        public void Tick(DateTime? currentZulu)
        {
            if (!_isStarted) return;
            
            var now = currentZulu ?? DateTime.UtcNow;
            if (now.Year < 2000) now = DateTime.UtcNow;
            
            if (_lastTick.Year < 2000) _lastTick = now;
            
            double deltaD = (now - _lastTick).TotalSeconds;

            if (IsPaused) 
            {
                _lastTick = now; // Don't accumulate when paused
                return;
            }

            if (deltaD < 1.0) return; // Wait until at least 1 full second has accumulated
            
            var delta = (int)deltaD;
            _lastTick = _lastTick.AddSeconds(delta); // Advance _lastTick strictly by the consumed integer seconds to keep the fractional remainder

            if (Services.Count == 0) return;

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
                        if (CurrentPhase == FlightPhase.Turnaround)
                        {
                            if (s.Name != "Deboarding" && s.Name != "Cargo/Luggage")
                            {
                                s.StatusMessage = LocalizationService.Translate("Wait Turnaround", "Attente Turnaround");
                                continue;
                            }
                            else if (s.Name == "Deboarding" && IsSeatbeltsOn)
                            {
                                s.StatusMessage = LocalizationService.Translate("Wait Seatbelts", "Attentes Signes");
                                continue;
                            }
                            else if (s.Name == "Cargo/Luggage" && IsBeaconOn)
                            {
                                s.StatusMessage = LocalizationService.Translate("Wait Beacon", "Attente Beacon");
                                continue;
                            }
                        }

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
                            changed = true;
                        }
                    }

                    if (shouldStart)
                    {
                        var boarding = Services.FirstOrDefault(x => x.Name == "Boarding");
                        var catering = Services.FirstOrDefault(x => x.Name == "Catering");
                        var cleaning = Services.FirstOrDefault(x => x.Name == "Cleaning");

                        if (s.Name == "Boarding")
                        {
                            bool cateringPending = catering != null && catering.State != GroundServiceState.Completed && catering.State != GroundServiceState.Skipped;
                            bool cleaningPending = cleaning != null && cleaning.State != GroundServiceState.Completed && cleaning.State != GroundServiceState.Skipped;
                            bool isLcc = cleaning != null && IsLowCost;

                            if (!isLcc && (cateringPending || cleaningPending))
                            {
                                shouldStart = false;
                                if (cateringPending && cleaningPending)
                                    s.StatusMessage = LocalizationService.Translate("Wait Clean/Cater", "Attt Nettoy/Cater.");
                                else if (cleaningPending)
                                    s.StatusMessage = LocalizationService.Translate("Wait Cleaning", "Attente Nettoyage");
                                else
                                    s.StatusMessage = LocalizationService.Translate("Wait Catering", "Attente Catering");
                            }
                        }
                        else if ((s.Name == "Cleaning" || s.Name == "Catering") && boarding != null && boarding.State != GroundServiceState.NotStarted && boarding.State != GroundServiceState.Skipped)
                        {
                            bool isLcc = IsLowCost;
                            
                            if (!isLcc)
                            {
                                // If boarding has started or finished, cleaning and catering can't happen anymore (for Legacy carriers only).
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
                    }

                    if (shouldStart)
                    {
                        if (CurrentPhase == FlightPhase.Turnaround)
                        {
                            if (s.Name != "Deboarding" && s.Name != "Cargo/Luggage")
                            {
                                s.StatusMessage = LocalizationService.Translate("Wait Turnaround", "Attente Turnaround");
                                continue;
                            }
                            else if (s.Name == "Deboarding" && IsSeatbeltsOn)
                            {
                                s.StatusMessage = LocalizationService.Translate("Wait Seatbelts", "Attentes Signes");
                                continue;
                            }
                            else if (s.Name == "Cargo/Luggage" && IsBeaconOn)
                            {
                                s.StatusMessage = LocalizationService.Translate("Wait Beacon", "Attente Beacon");
                                continue;
                            }
                            else if (EngineMaxN1 > 5.0 && !_hasEmittedN1Warning)
                            {
                                _hasEmittedN1Warning = true;
                                OnOpsLog?.Invoke(LocalizationService.Translate("[WARNING] Ground Ops started while engines are running! This is extremely dangerous. Penalty applied.", "[WARNING] Opération lancée moteurs allumés ! Très dangereux. Pénalité affectée."));
                                OnPenaltyTriggered?.Invoke(-50, "Engines running during Turnaround OPs");
                            }
                        }

                        s.State = GroundServiceState.InProgress;
                        s.StatusMessage = LocalizationService.Translate("In Progress", "En cours");
                        changed = true;
                        
                        // Virtual Actors Logging Start Events
                        string actor = GetActorForService(s.Name);
                        string startMsg = GetStartMessageForService(s.Name);
                        OnOpsLog?.Invoke(LocalizationService.Translate($"[{actor}] {startMsg}", $"[{actor}] {startMsg}"));
                        OnServiceStarted?.Invoke(s.Name);
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
                    double chance = 0.0; // EventProbabilityPercent * 0.000005 * delta; // Bug 23: Debranch events
                    if (!s.HasBeenDelayed && s.State != GroundServiceState.Delayed && chance > 0 && _rnd.NextDouble() < chance) 
                    {
                        s.State = GroundServiceState.Delayed;
                        s.HasBeenDelayed = true;
                        
                        string eventDesc = "Perturbation inopinée";
                        int additionalDelay = _rnd.Next(30, 90);
                        
                        if (_delayEvents.TryGetValue(s.Name, out var eventsList) && eventsList.Count > 0)
                        {
                            var evt = eventsList[_rnd.Next(eventsList.Count)];
                            eventDesc = evt.Description;
                            additionalDelay = _rnd.Next(evt.MinDelaySec, evt.MaxDelaySec);

                            if (s.Name == "Boarding" && evt.DescriptionEn == "Missing passenger")
                            {
                                double efficRatio = Math.Max(50.0, CurrentCrewEfficiency) / 100.0;
                                additionalDelay = (int)(additionalDelay / efficRatio); // Un bon crew résout ça plus vite
                                
                                if (CurrentCrewEfficiency >= 85)
                                    OnOperationBonusTriggered?.Invoke(15, LocalizationService.Translate("Crew Efficiency: Swiftly found missing passenger", "Efficacité Équipage: Passager manquant géré rapidement"));
                                else if (CurrentCrewEfficiency < 65)
                                    OnPenaltyTriggered?.Invoke(-10, LocalizationService.Translate("Crew Inefficiency: Failed to quickly find passenger", "Inefficacité Équipage: Passager manquant géré lentement"));
                            }
                            else if (s.Name == "Cleaning")
                            {
                                double efficRatio = Math.Max(50.0, CurrentCrewEfficiency) / 100.0;
                                additionalDelay = (int)(additionalDelay / efficRatio);
                                
                                if (CurrentCrewEfficiency >= 85)
                                    OnOperationBonusTriggered?.Invoke(10, LocalizationService.Translate("Crew Efficiency: Swiftly resolved cabin delay", "Efficacité Équipage: Incident cabine géré rapidement"));
                            }
                        }

                        s.ActiveDelayEvent = eventDesc;
                        s.DelayAddedSec += additionalDelay;
                        s.StatusMessage = LocalizationService.Translate($"Delay: {eventDesc} (+{(int)Math.Ceiling(additionalDelay/60.0)}m)", $"Retard: {eventDesc} (+{(int)Math.Ceiling(additionalDelay/60.0)}m)");
                        changed = true;
                        
                        string actor = GetActorForService(s.Name);
                        OnOpsLog?.Invoke(LocalizationService.Translate($"[{actor}] Ground Op issue: {eventDesc} (+{(int)Math.Ceiling(additionalDelay/60.0)} min)", $"[{actor}] Problème sur l'escale : {eventDesc} (+{(int)Math.Ceiling(additionalDelay/60.0)} min)"));
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
            if (name.Contains("PNC", StringComparison.OrdinalIgnoreCase)) return "CABIN CREW";

            switch (name.ToLower())
            {
                case "board":
                case "boarding":
                    return "GATE AGENT";
                case "catering":
                case "cleaning":

                default: // Fuel, Cargo, Water
                    return "RAMP AGENT";
            }
        }

        private string GetStartMessageForService(string name)
        {
            if (name.Contains("PNC", StringComparison.OrdinalIgnoreCase)) return "Cabin Crew starting turnaround chores.";

            switch (name.ToLower())
            {
                case "boarding": return "We are starting general boarding at the terminal.";
                case "catering": return "Loading the galley catering carts.";
                case "cleaning": return "Cleaning crew has entered the cabin.";

                case "cargo": return "Starting lower deck cargo loading.";
                case "water/waste": return "Servicing the blue water logic systems.";
                default: return $"{name} is underway.";
            }
        }

        private string GetEndMessageForService(string name)
        {
            if (name.Contains("PNC", StringComparison.OrdinalIgnoreCase)) return "Cabin is clean, chores completed.";

            switch (name.ToLower())
            {
                case "boarding": return "Passenger count verified. Cabin is secured.";
                case "catering": return "All trolleys are locked. Catering is completed.";
                case "cleaning": return "Cabin interior is tidy and ready for flight.";

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

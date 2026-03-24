using System;
using System.Collections.Generic;
using System.Linq;
using FlightSupervisor.UI.Models.SimBrief;

namespace FlightSupervisor.UI.Services
{
    public enum GroundServiceState { NotStarted, InProgress, Delayed, Completed, Skipped }

    public enum GroundOpsSpeed { Realistic, Short, Instant }

    public class GroundService
    {
        public string Name { get; set; } = "";
        public GroundServiceState State { get; set; } = GroundServiceState.NotStarted;
        public int TotalDurationSec { get; set; }
        public int ElapsedSec { get; set; }
        public int DelayAddedSec { get; set; }
        public string StatusMessage { get; set; } = "En attente";
        public string ActiveDelayEvent { get; set; } = "";
        public bool IsOptional { get; set; }
        public bool HasBeenDelayed { get; set; } = false;

        public int RemainingSec => Math.Max(0, (TotalDurationSec + DelayAddedSec) - ElapsedSec);
        public int ProgressPercent => (TotalDurationSec + DelayAddedSec) == 0 ? 100 : (int)Math.Min(100, Math.Max(0, ((double)ElapsedSec / (TotalDurationSec + DelayAddedSec)) * 100));
    }

    public class DelayEvent
    {
        public string Description { get; set; } = "";
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

        private static readonly Dictionary<string, List<DelayEvent>> _delayEvents = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Refuel", new List<DelayEvent> {
                new() { Description = "Problème de jauge", MinDelaySec = 120, MaxDelaySec = 300 },
                new() { Description = "Camion en retard", MinDelaySec = 180, MaxDelaySec = 480 },
                new() { Description = "Procédure anti-déversement", MinDelaySec = 300, MaxDelaySec = 600 }
            }},
            { "Boarding", new List<DelayEvent> {
                new() { Description = "Passager manquant", MinDelaySec = 300, MaxDelaySec = 900 },
                new() { Description = "Attente assistance PMR", MinDelaySec = 180, MaxDelaySec = 420 },
                new() { Description = "Problème passerelle", MinDelaySec = 240, MaxDelaySec = 600 },
                new() { Description = "Débarquement bagage", MinDelaySec = 300, MaxDelaySec = 480 }
            }},
            { "Cargo", new List<DelayEvent> {
                new() { Description = "Fret hors dimension", MinDelaySec = 300, MaxDelaySec = 720 },
                new() { Description = "Panne du chargeur", MinDelaySec = 300, MaxDelaySec = 600 },
                new() { Description = "Retard bagages", MinDelaySec = 180, MaxDelaySec = 480 }
            }},
            { "Catering", new List<DelayEvent> {
                new() { Description = "Chariots repas manquants", MinDelaySec = 300, MaxDelaySec = 600 },
                new() { Description = "Contrôle sûreté chariot", MinDelaySec = 180, MaxDelaySec = 360 }
            }},
            { "Cleaning", new List<DelayEvent> {
                new() { Description = "Nettoyage approfondi", MinDelaySec = 180, MaxDelaySec = 420 },
                new() { Description = "Manque de personnel", MinDelaySec = 240, MaxDelaySec = 480 }
            }},
            { "Water/Waste", new List<DelayEvent> {
                new() { Description = "Fuite tuyau raccordement", MinDelaySec = 120, MaxDelaySec = 360 },
                new() { Description = "Véhicule indisponible", MinDelaySec = 300, MaxDelaySec = 720 }
            }}
        };

        public event Action? OnOpsCompleted;
        public event Action? OnOpsUpdated;
        public event Action<string>? OnOpsLog;

        public void InitializeFromSimBrief(SimBriefResponse? sb)
        {
            Services.Clear();
            int pax = 0;
            int.TryParse(sb?.Weights?.PaxCount, out pax);
            int fuel = 0;
            int.TryParse(sb?.Fuel?.PlanRamp, out fuel);

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

            Services.Add(new GroundService { Name = "Refuel", TotalDurationSec = applyTime(Math.Max(60, fuel / 50)), IsOptional = false });
            Services.Add(new GroundService { Name = "Boarding", TotalDurationSec = applyTime(Math.Max(120, pax * 6)), IsOptional = false });
            Services.Add(new GroundService { Name = "Cargo", TotalDurationSec = applyTime(Math.Max(120, pax * 5)), IsOptional = false });
            Services.Add(new GroundService { Name = "Catering", TotalDurationSec = applyTime(180), IsOptional = true });
            Services.Add(new GroundService { Name = "Cleaning", TotalDurationSec = applyTime(300), IsOptional = true });
            Services.Add(new GroundService { Name = "Water/Waste", TotalDurationSec = applyTime(150), IsOptional = true });
            
            _isStarted = false;
        }

        public void StartOps()
        {
            if (_isStarted) return;
            _isStarted = true;
            _lastTick = DateTime.UtcNow;
            foreach (var s in Services) { s.State = GroundServiceState.InProgress; s.StatusMessage = "En cours"; }
        }

        public void SkipService(string name)
        {
            var s = Services.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (s != null && s.IsOptional && s.State != GroundServiceState.Completed)
            {
                s.State = GroundServiceState.Skipped;
                s.StatusMessage = "Ignoré par le Commandant";
                s.ElapsedSec = s.TotalDurationSec + s.DelayAddedSec;
                OnOpsUpdated?.Invoke();
            }
        }

        public void Tick()
        {
            if (!_isStarted) return;
            var now = DateTime.UtcNow;
            var delta = (int)(now - _lastTick).TotalSeconds;
            if (delta <= 0) return;
            _lastTick = now;

            bool allDone = true;
            bool changed = false;

            foreach (var s in Services)
            {
                if (s.State == GroundServiceState.Completed || s.State == GroundServiceState.Skipped) continue;

                allDone = false;
                s.ElapsedSec += delta;

                if (s.ElapsedSec >= s.TotalDurationSec + s.DelayAddedSec)
                {
                    s.State = GroundServiceState.Completed;
                    s.StatusMessage = "Terminé";
                    changed = true;
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
                        s.StatusMessage = $"Retard: {eventDesc} (+{(additionalDelay/60)}m)";
                        changed = true;
                        
                        OnOpsLog?.Invoke($"[GROUND OPS] {s.Name} Delayed : {eventDesc} (+{(additionalDelay/60)} min)");
                    }
                    // Recover from delay if progressing normally again
                    else if (s.State == GroundServiceState.Delayed && _rnd.NextDouble() < 0.05)
                    {
                        s.State = GroundServiceState.InProgress;
                        s.ActiveDelayEvent = "";
                        s.StatusMessage = "En cours";
                        changed = true;
                        OnOpsLog?.Invoke($"[GROUND OPS] {s.Name} : Reprise de l'opération.");
                    }
                }
            }

            if (changed) OnOpsUpdated?.Invoke();
            if (allDone) { _isStarted = false; OnOpsCompleted?.Invoke(); }
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
                    s.StatusMessage = "ABORTED!";
                    s.ElapsedSec = s.TotalDurationSec + s.DelayAddedSec;
                }
            }
            _isStarted = false;
            OnOpsUpdated?.Invoke();
        }
    }
}

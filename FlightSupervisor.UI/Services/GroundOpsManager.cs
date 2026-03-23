using System;
using System.Collections.Generic;
using System.Linq;
using FlightSupervisor.UI.Models.SimBrief;

namespace FlightSupervisor.UI.Services
{
    public enum GroundServiceState { NotStarted, InProgress, Delayed, Completed, Skipped }

    public class GroundService
    {
        public string Name { get; set; } = "";
        public GroundServiceState State { get; set; } = GroundServiceState.NotStarted;
        public int TotalDurationSec { get; set; }
        public int ElapsedSec { get; set; }
        public int DelayAddedSec { get; set; }
        public string StatusMessage { get; set; } = "En attente";
        public bool IsOptional { get; set; }

        public int RemainingSec => Math.Max(0, (TotalDurationSec + DelayAddedSec) - ElapsedSec);
        public int ProgressPercent => (TotalDurationSec + DelayAddedSec) == 0 ? 100 : (int)Math.Min(100, Math.Max(0, ((double)ElapsedSec / (TotalDurationSec + DelayAddedSec)) * 100));
    }

    public class GroundOpsManager
    {
        public List<GroundService> Services { get; private set; } = new();
        private Random _rnd = new Random();
        private bool _isStarted = false;
        private DateTime _lastTick;

        public event Action? OnOpsCompleted;
        public event Action? OnOpsUpdated;

        public void InitializeFromSimBrief(SimBriefResponse? sb)
        {
            Services.Clear();
            int pax = 0;
            int.TryParse(sb?.Weights?.PaxCount, out pax);
            int fuel = 0;
            int.TryParse(sb?.Fuel?.PlanRamp, out fuel);

            Services.Add(new GroundService { Name = "Refuel", TotalDurationSec = Math.Max(60, fuel / 50), IsOptional = false });
            Services.Add(new GroundService { Name = "Boarding", TotalDurationSec = Math.Max(120, pax * 6), IsOptional = false });
            Services.Add(new GroundService { Name = "Cargo", TotalDurationSec = Math.Max(120, pax * 5), IsOptional = false });
            Services.Add(new GroundService { Name = "Catering", TotalDurationSec = 180, IsOptional = true });
            Services.Add(new GroundService { Name = "Cleaning", TotalDurationSec = 300, IsOptional = true });
            Services.Add(new GroundService { Name = "Water/Waste", TotalDurationSec = 150, IsOptional = true });
            
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
                    // Random Delay chance (max 3 mins length as requested yesterday)
                    if (s.State != GroundServiceState.Delayed && _rnd.NextDouble() < 0.005) 
                    {
                        s.State = GroundServiceState.Delayed;
                        int additionalDelay = _rnd.Next(30, 180);
                        s.DelayAddedSec += additionalDelay;
                        s.StatusMessage = $"Retard : +{(additionalDelay/60)} min";
                        changed = true;
                    }
                    // Recover from delay if progressing normally again
                    else if (s.State == GroundServiceState.Delayed && _rnd.NextDouble() < 0.05)
                    {
                        s.State = GroundServiceState.InProgress;
                        s.StatusMessage = "En cours";
                        changed = true;
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

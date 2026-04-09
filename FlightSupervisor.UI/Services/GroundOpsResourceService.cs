using System;
using System.Linq;

namespace FlightSupervisor.UI.Services
{
    public class GroundOpsResourceService
    {
        private readonly GroundOpsManager _groundOpsManager;
        private readonly CabinManager _cabinManager;

        public GroundOpsResourceService(GroundOpsManager groundOpsManager, CabinManager cabinManager)
        {
            _groundOpsManager = groundOpsManager;
            _cabinManager = cabinManager;
        }

        public void Tick(int deltaMs)
        {
            // The UI timer ticks at 500ms, but this method allows flexibility
            // We calculate the rate based on a "per second" metric then map to deltaMs.
            double tickRatio = deltaMs / 1000.0;

            var caterSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Catering");
            if (caterSvc != null && caterSvc.State == GroundServiceState.InProgress)
            {
                double rate = 100.0 / Math.Max(1, caterSvc.TotalDurationSec);
                _cabinManager.CateringCompletion = Math.Min(100.0, _cabinManager.CateringCompletion + (rate * tickRatio));
            }

            var cleanSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Cleaning" || s.Name == "PNC Chores");
            if (cleanSvc != null && cleanSvc.State == GroundServiceState.InProgress)
            {
                double rate = 100.0 / Math.Max(1, cleanSvc.TotalDurationSec);
                _cabinManager.CabinCleanliness = Math.Min(100.0, _cabinManager.CabinCleanliness + (rate * tickRatio));
            }

            var waterSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Water/Waste");
            if (waterSvc != null && waterSvc.State == GroundServiceState.InProgress)
            {
                double rate = 100.0 / Math.Max(1, waterSvc.TotalDurationSec);
                _cabinManager.WaterLevel = Math.Min(100.0, _cabinManager.WaterLevel + (rate * tickRatio));
                _cabinManager.WasteLevel = Math.Max(0.0, _cabinManager.WasteLevel - (rate * tickRatio));
            }

            var cargoSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Cargo/Luggage" || s.Name == "Cargo");
            if (cargoSvc != null && cargoSvc.State == GroundServiceState.InProgress)
            {
                double rate = 100.0 / Math.Max(1, cargoSvc.TotalDurationSec);
                _cabinManager.BaggageCompletion = Math.Min(100.0, _cabinManager.BaggageCompletion + (rate * tickRatio));
            }

            var boardSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Boarding");
            if (boardSvc != null && (boardSvc.State == GroundServiceState.InProgress || boardSvc.State == GroundServiceState.Completed))
            {
                int expectedBoarded = boardSvc.State == GroundServiceState.Completed 
                    ? _cabinManager.PassengerManifest.Count 
                    : (int)(_cabinManager.PassengerManifest.Count * ((double)boardSvc.ElapsedSec / Math.Max(1, boardSvc.TotalDurationSec)));
                
                int currentlyBoarded = _cabinManager.PassengerManifest.Count(p => p.IsBoarded);
                
                if (expectedBoarded > currentlyBoarded)
                {
                    _cabinManager.BoardPassenger(expectedBoarded - currentlyBoarded);
                }
            }

            var deboardSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Deboarding");
            if (deboardSvc != null && (deboardSvc.State == GroundServiceState.InProgress || deboardSvc.State == GroundServiceState.Completed))
            {
                // Only deboard the previous manifest if it still has passengers. 
                // DO NOT target the new manifest, otherwise we instantly deboard Leg 2 pax as they board.
                if (_cabinManager.PreviousLegManifest.Any(p => p.IsBoarded))
                {
                    int expectedRemaining = deboardSvc.State == GroundServiceState.Completed 
                        ? 0 
                        : (int)(_cabinManager.PreviousLegManifest.Count * (1.0 - ((double)deboardSvc.ElapsedSec / Math.Max(1, deboardSvc.TotalDurationSec))));
                    
                    int currentlyBoarded = _cabinManager.PreviousLegManifest.Count(p => p.IsBoarded);
                    
                    if (currentlyBoarded > expectedRemaining)
                    {
                        var toDeboard = currentlyBoarded - expectedRemaining;
                        var boarded = _cabinManager.PreviousLegManifest.Where(p => p.IsBoarded).TakeLast(toDeboard).ToList();
                        foreach (var p in boarded)
                        {
                            p.IsBoarded = false;
                            p.IsSeatbeltFastened = false;
                        }
                    }
                }

            }
        }
    }
}

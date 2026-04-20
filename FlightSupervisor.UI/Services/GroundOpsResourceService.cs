using System;
using System.Linq;

namespace FlightSupervisor.UI.Services
{
    public class GroundOpsResourceService
    {
        private readonly GroundOpsManager _groundOpsManager;
        private readonly CabinManager _cabinManager;

        private int _lastCateringSec = 0;
        private int _lastCleanSec = 0;
        private int _lastWaterSec = 0;
        private int _lastFuelSec = 0;
        private int _lastCargoSec = 0;

        public GroundOpsResourceService(GroundOpsManager groundOpsManager, CabinManager cabinManager)
        {
            _groundOpsManager = groundOpsManager;
            _cabinManager = cabinManager;
        }

        public void Reset()
        {
            _lastCateringSec = 0;
            _lastCleanSec = 0;
            _lastWaterSec = 0;
            _lastFuelSec = 0;
            _lastCargoSec = 0;
        }

        public void Tick(int deltaMs)
        {
            var caterSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Catering");
            if (caterSvc != null && (caterSvc.State == GroundServiceState.InProgress || caterSvc.State == GroundServiceState.Completed))
            {
                if (caterSvc.ElapsedSec < _lastCateringSec) _lastCateringSec = 0;
                int delta = caterSvc.ElapsedSec - _lastCateringSec;
                if (delta > 0)
                {
                    double remainingSec = Math.Max(1.0, caterSvc.TotalDurationSec - _lastCateringSec);
                    double completionToAdd = (100.0 - _cabinManager.CateringCompletion) * delta / remainingSec;
                    _cabinManager.CateringCompletion = Math.Min(100.0, _cabinManager.CateringCompletion + completionToAdd);
                    _lastCateringSec = Math.Min(caterSvc.ElapsedSec, caterSvc.TotalDurationSec);
                }
            }

            var cleanSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Cleaning" || s.Name == "Cabin Cleaning" || s.Name == "PNC Chores" || s.Name == "Light Cleaning" || s.Name == "Deep Cleaning");
            if (cleanSvc != null && (cleanSvc.State == GroundServiceState.InProgress || cleanSvc.State == GroundServiceState.Completed))
            {
                if (cleanSvc.ElapsedSec < _lastCleanSec) _lastCleanSec = 0;
                int delta = cleanSvc.ElapsedSec - _lastCleanSec;
                if (delta > 0)
                {
                    double remainingSec = Math.Max(1.0, cleanSvc.TotalDurationSec - _lastCleanSec);
                    double completionToAdd = (98.5 - _cabinManager.CabinCleanliness) * delta / remainingSec;
                    _cabinManager.CabinCleanliness = Math.Min(98.5, _cabinManager.CabinCleanliness + completionToAdd);
                    _lastCleanSec = Math.Min(cleanSvc.ElapsedSec, cleanSvc.TotalDurationSec);
                }
            }

            var waterSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Water/Waste");
            if (waterSvc != null && (waterSvc.State == GroundServiceState.InProgress || waterSvc.State == GroundServiceState.Completed))
            {
                if (waterSvc.ElapsedSec < _lastWaterSec) _lastWaterSec = 0;
                int delta = waterSvc.ElapsedSec - _lastWaterSec;
                if (delta > 0)
                {
                    double remainingSec = Math.Max(1.0, waterSvc.TotalDurationSec - _lastWaterSec);
                    double waterToAdd = (100.0 - _cabinManager.WaterLevel) * delta / remainingSec;
                    double wasteToRemove = _cabinManager.WasteLevel * delta / remainingSec;
                    _cabinManager.WaterLevel = Math.Min(100.0, _cabinManager.WaterLevel + waterToAdd);
                    _cabinManager.WasteLevel = Math.Max(0.0, _cabinManager.WasteLevel - wasteToRemove);
                    _lastWaterSec = Math.Min(waterSvc.ElapsedSec, waterSvc.TotalDurationSec);
                }
            }

            var fuelSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Refueling");
            if (fuelSvc != null && (fuelSvc.State == GroundServiceState.InProgress || fuelSvc.State == GroundServiceState.Completed))
            {
                if (fuelSvc.ElapsedSec < _lastFuelSec) _lastFuelSec = 0;
                int delta = fuelSvc.ElapsedSec - _lastFuelSec;
                if (delta > 0)
                {
                    double remainingSec = Math.Max(1.0, fuelSvc.TotalDurationSec - _lastFuelSec);
                    double fuelToAdd = (100.0 - _cabinManager.VirtualFuelPercentage) * delta / remainingSec;
                    _cabinManager.VirtualFuelPercentage = Math.Min(100.0, _cabinManager.VirtualFuelPercentage + fuelToAdd);
                    _lastFuelSec = Math.Min(fuelSvc.ElapsedSec, fuelSvc.TotalDurationSec);
                }
            }

            var cargoSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Cargo Loading" || s.Name == "Cargo Unloading" || s.Name == "Cargo/Luggage" || s.Name == "Cargo");
            if (cargoSvc != null && (cargoSvc.State == GroundServiceState.InProgress || cargoSvc.State == GroundServiceState.Completed))
            {
                if (cargoSvc.ElapsedSec < _lastCargoSec) _lastCargoSec = 0;
                int delta = cargoSvc.ElapsedSec - _lastCargoSec;
                if (delta > 0)
                {
                    double remainingSec = Math.Max(1.0, cargoSvc.TotalDurationSec - _lastCargoSec);
                    double cargoToAdd = (100.0 - _cabinManager.BaggageCompletion) * delta / remainingSec;
                    _cabinManager.BaggageCompletion = Math.Min(100.0, _cabinManager.BaggageCompletion + cargoToAdd);
                    _lastCargoSec = Math.Min(cargoSvc.ElapsedSec, cargoSvc.TotalDurationSec);
                }
            }

            var boardSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Boarding");
            if (boardSvc != null && _cabinManager.PassengerManifest != null)
            {
                if (boardSvc.State == GroundServiceState.InProgress)
                {
                    double ratio = (double)boardSvc.ElapsedSec / Math.Max(1, boardSvc.TotalDurationSec);
                    ratio = Math.Max(0.0, Math.Min(1.0, ratio)); // strict 0 to 1 clamping
                    int expectedBoarded = (int)(_cabinManager.PassengerManifest.Count * ratio);
                    int currentlyBoarded = _cabinManager.PassengerManifest.Count(p => p != null && p.IsBoarded);
                    
                    if (expectedBoarded > currentlyBoarded)
                    {
                        var toAdd = expectedBoarded - currentlyBoarded;
                        _cabinManager.BoardPassenger(toAdd);
                    }
                }
                else if (boardSvc.State == GroundServiceState.Completed)
                {
                    int currentlyBoarded = _cabinManager.PassengerManifest.Count(p => p != null && p.IsBoarded);
                    if (currentlyBoarded < _cabinManager.PassengerManifest.Count)
                    {
                        _cabinManager.BoardPassenger(_cabinManager.PassengerManifest.Count - currentlyBoarded);
                    }
                }
            }

            var deboardSvc = _groundOpsManager.Services.FirstOrDefault(s => s.Name == "Deboarding");
            
            var targetManifest = (_cabinManager.PreviousLegManifest != null && _cabinManager.PreviousLegManifest.Any(p => p != null && p.IsBoarded)) ? _cabinManager.PreviousLegManifest : _cabinManager.PassengerManifest;

            if (deboardSvc != null && targetManifest != null && targetManifest.Any(p => p != null && p.IsBoarded))
            {
                // Prevent Deboarding from ripping out passengers if we have already moved on to the Boarding phase
                bool isBoardingActive = boardSvc != null && (boardSvc.State == GroundServiceState.InProgress || boardSvc.State == GroundServiceState.Completed);
                
                if (!isBoardingActive)
                {
                    if (deboardSvc.State == GroundServiceState.InProgress)
                    {
                        double ratio = (double)deboardSvc.ElapsedSec / Math.Max(1, deboardSvc.TotalDurationSec);
                        ratio = Math.Max(0.0, Math.Min(1.0, ratio));
                        int expectedRemaining = (int)(targetManifest.Count * (1.0 - ratio));
                        int currentlyBoarded = targetManifest.Count(p => p != null && p.IsBoarded);
                        
                        if (currentlyBoarded > expectedRemaining)
                        {
                            int toDeboard = currentlyBoarded - expectedRemaining;
                            _cabinManager.DeboardPassenger(toDeboard);
                        }
                    }
                    else if (deboardSvc.State == GroundServiceState.Completed)
                    {
                        int currentlyBoarded = targetManifest.Count(p => p != null && p.IsBoarded);
                        if (currentlyBoarded > 0)
                        {
                            _cabinManager.DeboardPassenger(currentlyBoarded);
                        }
                    }
                }
            }
        }
    }
}

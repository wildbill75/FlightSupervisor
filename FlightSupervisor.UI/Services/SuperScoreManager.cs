using System;

namespace FlightSupervisor.UI.Services
{
    public enum ScoreCategory
    {
        FlightPhaseFlows,   // FLIGHT PHASE FLOWS
        Communication,      // COMMUNICATION
        Airmanship,         // AIRMANSHIP
        Maintenance,        // MAINTENANCE
        AbnormalOperations, // ABNORMAL OPERATIONS
        PassengerExperience // PASSENGER EXPERIENCE (vie à bord)
    }

    public class ScoreEvent
    {
        public int Amount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public ScoreCategory Category { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class SuperScoreManager
    {
        public List<ScoreEvent> FlightEvents { get; private set; } = new List<ScoreEvent>();
        
        public int CurrentScore { get; private set; } = 1000;
        
        public int FlightPhaseFlowsPoints { get; private set; } = 0;
        public int CommunicationPoints { get; private set; } = 0;
        public int AirmanshipPoints { get; private set; } = 0;
        public int MaintenancePoints { get; private set; } = 0;
        public int AbnormalOperationsPoints { get; private set; } = 0;
        public int PassengerExperiencePoints { get; private set; } = 0;
        public event Action<int, int, string>? OnScoreChanged; // NewScore, Delta, Reason

        private bool _bonusApEngaged = false;
        private FlightPhaseManager _phaseManager;

        public SuperScoreManager(FlightPhaseManager phaseManager, SimConnectService simConnect)
        {
            _phaseManager = phaseManager;

            simConnect.OnAutopilotReceived += ap => {
                if (ap && !_bonusApEngaged && 
                   (_phaseManager.CurrentPhase == FlightPhase.InitialClimb || _phaseManager.CurrentPhase == FlightPhase.Climb))
                {
                    _bonusApEngaged = true;
                    AddScore(50, "Autopilot Engaged Smoothly");
                }
            };
        }

        public void AddScore(int amount, string reason, ScoreCategory category = ScoreCategory.Airmanship)
        {
            CurrentScore += amount;
            
            switch (category)
            {
                case ScoreCategory.FlightPhaseFlows: FlightPhaseFlowsPoints += amount; break;
                case ScoreCategory.Communication: CommunicationPoints += amount; break;
                case ScoreCategory.Airmanship: AirmanshipPoints += amount; break;
                case ScoreCategory.Maintenance: MaintenancePoints += amount; break;
                case ScoreCategory.AbnormalOperations: AbnormalOperationsPoints += amount; break;
                case ScoreCategory.PassengerExperience: PassengerExperiencePoints += amount; break;
            }

            FlightEvents.Add(new ScoreEvent { Amount = amount, Reason = reason, Category = category });

            OnScoreChanged?.Invoke(CurrentScore, amount, reason);
        }

        public void CancelFlight(string reason)
        {
            CurrentScore = 0;
            OnScoreChanged?.Invoke(CurrentScore, -1000, reason);
        }
        
        public void Reset()
        {
            CurrentScore = 1000;
            FlightPhaseFlowsPoints = 0;
            CommunicationPoints = 0;
            AirmanshipPoints = 0;
            MaintenancePoints = 0;
            AbnormalOperationsPoints = 0;
            PassengerExperiencePoints = 0;
            _bonusApEngaged = false;
            FlightEvents.Clear();
            OnScoreChanged?.Invoke(CurrentScore, 0, "Flight Reset");
        }
    }
}

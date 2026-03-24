using System;

namespace FlightSupervisor.UI.Services
{
    public enum ScoreCategory
    {
        Safety,
        Comfort,
        Maintenance,
        Operations
    }

    public class SuperScoreManager
    {
        public int CurrentScore { get; private set; } = 1000;
        
        public int SafetyPoints { get; private set; } = 0;
        public int ComfortPoints { get; private set; } = 0;
        public int MaintenancePoints { get; private set; } = 0;
        public int OperationsPoints { get; private set; } = 0;
        public event Action<int, int, string>? OnScoreChanged; // NewScore, Delta, Reason

        private bool _bonusApEngaged = false;
        private FlightPhaseManager _phaseManager;

        public SuperScoreManager(FlightPhaseManager phaseManager, SimConnectService simConnect)
        {
            _phaseManager = phaseManager;

            _phaseManager.OnPenaltyTriggered += msg => {
                int penalty = -100;
                ScoreCategory category = ScoreCategory.Safety;
                
                if (msg.Contains("Overspeed: Aircraft exceeded 250")) { penalty = -200; category = ScoreCategory.Safety; }
                else if (msg.Contains("Unstable Approach")) { penalty = -500; category = ScoreCategory.Safety; }
                else if (msg.Contains("Excessive Bank")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Excessive Pitch")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Landing Lights")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Taxi Lights")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Strobes or Landing Lights ON during ground ops")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Taxiing without Taxi Lights ON")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Severe Hard Landing")) { penalty = -300; category = ScoreCategory.Maintenance; }
                else if (msg.Contains("Hard Landing")) { penalty = -150; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Butter Landing")) { penalty = 150; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Normal Landing")) { penalty = 50; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Comfort Violation: Tight turn")) { penalty = -5; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Comfort Violation: Harsh braking")) { penalty = -5; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Taxi Overspeed")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Comfort Violation: Steep Bank Angle")) { penalty = -10; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Comfort Violation: High Vertical Speed")) { penalty = -10; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Comfort Violation: Uncomfortable Pitch Angle")) { penalty = -10; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Poor Line-up Configuration")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Line-up Configuration Bonus")) { penalty = 50; category = ScoreCategory.Safety; }
                else if (msg.Contains("Extreme Crosswind Landing")) { penalty = 150; category = ScoreCategory.Safety; }
                else if (msg.Contains("Great Crosswind Landing")) { penalty = 100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Nice Crosswind Landing")) { penalty = 50; category = ScoreCategory.Safety; }
                else if (msg.Contains("Short Landing")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Float Landing")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Perfect Touchdown Zone")) { penalty = 50; category = ScoreCategory.Safety; }
                else if (msg.Contains("Centerline Deviation")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Perfect Centerline")) { penalty = 50; category = ScoreCategory.Safety; }
                else if (msg.Contains("Crosswind Bonus Cancelled")) { penalty = 0; category = ScoreCategory.Safety; }
                else if (msg.Contains("Gear Retraction Bonus")) { penalty = 50; category = ScoreCategory.Safety; }
                else if (msg.Contains("Abnormal Gear Deployment")) { penalty = -200; category = ScoreCategory.Maintenance; }
                else if (msg.Contains("In-Flight Engine Failure")) { penalty = -500; category = ScoreCategory.Maintenance; }
                else if (msg.Contains("Cabin Incident")) { penalty = -50; category = ScoreCategory.Comfort; }
                
                AddScore(penalty, msg, category);
            };

            simConnect.OnAutopilotReceived += ap => {
                if (ap && !_bonusApEngaged && 
                   (_phaseManager.CurrentPhase == FlightPhase.InitialClimb || _phaseManager.CurrentPhase == FlightPhase.Climb))
                {
                    _bonusApEngaged = true;
                    AddScore(50, "Autopilot Engaged Smoothly");
                }
            };
        }

        public void AddScore(int amount, string reason, ScoreCategory category = ScoreCategory.Safety)
        {
            CurrentScore += amount;
            
            switch (category)
            {
                case ScoreCategory.Safety: SafetyPoints += amount; break;
                case ScoreCategory.Comfort: ComfortPoints += amount; break;
                case ScoreCategory.Maintenance: MaintenancePoints += amount; break;
                case ScoreCategory.Operations: OperationsPoints += amount; break;
            }

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
            SafetyPoints = 0;
            ComfortPoints = 0;
            MaintenancePoints = 0;
            OperationsPoints = 0;
            _bonusApEngaged = false;
            OnScoreChanged?.Invoke(CurrentScore, 0, "Flight Reset");
        }
    }
}

using System;

namespace FlightSupervisor.UI.Services
{
    public class SuperScoreManager
    {
        public int CurrentScore { get; private set; } = 1000;
        public event Action<int, int, string>? OnScoreChanged; // NewScore, Delta, Reason

        private bool _bonusApEngaged = false;
        private FlightPhaseManager _phaseManager;

        public SuperScoreManager(FlightPhaseManager phaseManager, SimConnectService simConnect)
        {
            _phaseManager = phaseManager;

            _phaseManager.OnPenaltyTriggered += msg => {
                int penalty = -100;
                if (msg.Contains("Overspeed: Aircraft exceeded 250")) penalty = -200;
                else if (msg.Contains("Unstable Approach")) penalty = -500;
                else if (msg.Contains("Excessive Bank")) penalty = -100;
                else if (msg.Contains("Excessive Pitch")) penalty = -100;
                else if (msg.Contains("Landing Lights")) penalty = -100;
                else if (msg.Contains("Taxi Lights")) penalty = -100;
                else if (msg.Contains("Severe Hard Landing")) penalty = -300;
                else if (msg.Contains("Hard Landing")) penalty = -150;
                else if (msg.Contains("Butter Landing")) penalty = 150;
                else if (msg.Contains("Normal Landing")) penalty = 50;
                else if (msg.Contains("Line-up Configuration Bonus")) penalty = 50;
                
                AddScore(penalty, msg);
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

        public void AddScore(int amount, string reason)
        {
            CurrentScore += amount;
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
            _bonusApEngaged = false;
            OnScoreChanged?.Invoke(CurrentScore, 0, "Flight Reset");
        }
    }
}

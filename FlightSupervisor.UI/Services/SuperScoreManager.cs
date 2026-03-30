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
                
                if (msg.Contains("Overspeed: Aircraft exceeded 250") || msg.Contains("Vitesse excessive")) { penalty = -200; category = ScoreCategory.Safety; }
                else if (msg.Contains("Structural Bank Limit") || msg.Contains("Limite Structurelle Roulis")) { penalty = -500; category = ScoreCategory.Safety; }
                else if (msg.Contains("Structural Pitch Limit") || msg.Contains("Limite Structurelle Assiette")) { penalty = -500; category = ScoreCategory.Safety; }
                else if (msg.Contains("Structural G-Force Limit") || msg.Contains("Limite Structurelle Force G")) { penalty = -500; category = ScoreCategory.Safety; }
                else if (msg.Contains("Severe G-Force") || msg.Contains("Force G Sévère")) { penalty = -200; category = ScoreCategory.Safety; }
                else if (msg.Contains("Uncomfortable G-Force") || msg.Contains("Force G Inconfortable")) { penalty = -10; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Unstable Approach") || msg.Contains("Approche Instable")) { penalty = -500; category = ScoreCategory.Safety; }
                else if (msg.Contains("Excessive Bank") || msg.Contains("Inclinaison")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Excessive Pitch") || msg.Contains("Assiette")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Landing Lights")) { penalty = -50; category = ScoreCategory.Safety; }
                else if (msg.Contains("Strobes or Landing Lights ON during ground ops") || msg.Contains("Strobes/Landing au sol")) { penalty = -20; category = ScoreCategory.Safety; }
                else if (msg.Contains("Severe Hard Landing") || msg.Contains("Impact TRÈS violent")) { penalty = -300; category = ScoreCategory.Maintenance; }
                else if (msg.Contains("Hard Landing") || msg.Contains("Impact rude")) { penalty = -150; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Butter Landing") || msg.Contains("Atterrissage Parfait")) { penalty = 150; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Normal Landing") || msg.Contains("Atterrissage Normal")) { penalty = 50; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Comfort Violation: Tight turn") || msg.Contains("Virage serré")) { penalty = -5; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Comfort Violation: Harsh braking") || msg.Contains("Freinage brusque")) { penalty = -5; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Taxi Overspeed") || msg.Contains("Excès vitesse Roulage")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Comfort Violation: Steep Bank Angle") || msg.Contains("Inclinaison inconfortable")) { penalty = -10; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Comfort Violation: High Vertical Speed") || msg.Contains("Vitesse Verticale trop forte")) { penalty = -10; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Comfort Violation: Uncomfortable Pitch Angle") || msg.Contains("Assiette inconfortable")) { penalty = -10; category = ScoreCategory.Comfort; }
                else if (msg.Contains("Unpressurized Takeoff") || msg.Contains("Décollage non pressurisé")) { penalty = -500; category = ScoreCategory.Safety; }
                else if (msg.Contains("Poor Line-up Configuration") || msg.Contains("Mauvaise Configuration Alignement")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Line-up Configuration Bonus") || msg.Contains("Bonus Alignement")) { penalty = 50; category = ScoreCategory.Safety; }
                else if (msg.Contains("Extreme Crosswind Landing") || msg.Contains("Atterrissage Extrême de Travers")) { penalty = 150; category = ScoreCategory.Safety; }
                else if (msg.Contains("Great Crosswind Landing") || msg.Contains("Superbe Atterrissage de Travers")) { penalty = 100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Nice Crosswind Landing") || msg.Contains("Bel Atterrissage de Travers")) { penalty = 50; category = ScoreCategory.Safety; }
                else if (msg.Contains("Strong Headwind") || msg.Contains("Fort Vent de Face")) { penalty = 50; category = ScoreCategory.Safety; }
                else if (msg.Contains("True Airmanship") || msg.Contains("Magistral")) { penalty = 200; category = ScoreCategory.Safety; }
                else if (msg.Contains("Good Airmanship") || msg.Contains("Bon Pilotage Manuel")) { penalty = 100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Short Landing") || msg.Contains("Atterrissage trop court")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Float Landing") || msg.Contains("Atterrissage trop long")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Perfect Touchdown Zone") || msg.Contains("Zone de Toucher Parfaite")) { penalty = 50; category = ScoreCategory.Safety; }
                else if (msg.Contains("Perfect Flare") || msg.Contains("Arrondi Parfait")) { penalty = 50; category = ScoreCategory.Safety; }
                else if (msg.Contains("10,000ft Climb Flow Complete") || msg.Contains("Procédure de montée 10,000ft Complète")) { penalty = 50; category = ScoreCategory.Operations; }
                else if (msg.Contains("Centerline Deviation") || msg.Contains("Déviation majeure")) { penalty = -100; category = ScoreCategory.Safety; }
                else if (msg.Contains("Perfect Centerline") || msg.Contains("Axe Parfait")) { penalty = 50; category = ScoreCategory.Safety; }
                else if (msg.Contains("Crosswind Bonus Cancelled") || msg.Contains("Bonus Vent de Travers Annulé")) { penalty = 0; category = ScoreCategory.Safety; }
                else if (msg.Contains("Gear Retraction Bonus") || msg.Contains("Bonus Train Rentré")) { penalty = 50; category = ScoreCategory.Safety; }
                else if (msg.Contains("Abnormal Gear Deployment") || msg.Contains("Sortie Anormale du Train")) { penalty = -200; category = ScoreCategory.Maintenance; }
                else if (msg.Contains("In-Flight Engine Failure") || msg.Contains("Panne Moteur en vol")) { penalty = -500; category = ScoreCategory.Maintenance; }
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
            SafetyPoints = 0;
            ComfortPoints = 0;
            MaintenancePoints = 0;
            OperationsPoints = 0;
            _bonusApEngaged = false;
            FlightEvents.Clear();
            OnScoreChanged?.Invoke(CurrentScore, 0, "Flight Reset");
        }
    }
}

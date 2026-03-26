using System;
using System.Collections.Generic;
using System.Linq;
using FlightSupervisor.UI.Models;

namespace FlightSupervisor.UI.Services
{
    public enum AchievementTier
    {
        Rookie,
        LineCaptain,
        CheckAirman,
        Dishonorable
    }

    public class BadgeDefinition
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public AchievementTier Tier { get; set; }
        public string Icon { get; set; }
        public string ColorClass { get; set; }
    }

    public class AchievementEngine
    {
        public static List<BadgeDefinition> AllBadges = new List<BadgeDefinition>
        {
            // TIER 1 : Rookie
            new BadgeDefinition { Id = "first_entry", Title = "First Entry", Description = "Completed your first flight log.", Tier = AchievementTier.Rookie, Icon = "menu_book", ColorClass = "text-sky-400" },
            new BadgeDefinition { Id = "butter_bread", Title = "Butter the Bread", Description = "Landed smoother than -150 fpm.", Tier = AchievementTier.Rookie, Icon = "flight_land", ColorClass = "text-sky-400" },
            new BadgeDefinition { Id = "swiss_watch", Title = "Swiss Watch", Description = "Arrived on or before scheduled time.", Tier = AchievementTier.Rookie, Icon = "schedule", ColorClass = "text-sky-400" },
            new BadgeDefinition { Id = "by_the_book", Title = "By the Book", Description = "Completed all ground operations without rushing.", Tier = AchievementTier.Rookie, Icon = "checklist_rtl", ColorClass = "text-sky-400" },

            // TIER 2 : Line Captain
            new BadgeDefinition { Id = "frequent_flyer", Title = "Frequent Flyer", Description = "Logged 50 successful flights.", Tier = AchievementTier.LineCaptain, Icon = "military_tech", ColorClass = "text-amber-400" },
            new BadgeDefinition { Id = "hand_of_god", Title = "The Hand of God", Description = "Over 10 minutes of manual flying in a single flight.", Tier = AchievementTier.LineCaptain, Icon = "front_hand", ColorClass = "text-amber-400" },
            new BadgeDefinition { Id = "company_man", Title = "Company Man", Description = "Earned a SuperScore over 1000 on a single sector.", Tier = AchievementTier.LineCaptain, Icon = "work", ColorClass = "text-amber-400" },
            new BadgeDefinition { Id = "safe_and_sound", Title = "Safe and Sound", Description = "Completed 10 flights in a row with zero safety infractions.", Tier = AchievementTier.LineCaptain, Icon = "verified_user", ColorClass = "text-amber-400" },
            new BadgeDefinition { Id = "go_around_flaps3", Title = "Go-Around, Flaps 3", Description = "Successfully executed a Go-Around.", Tier = AchievementTier.LineCaptain, Icon = "autorenew", ColorClass = "text-amber-400" },

            // TIER 3 : Check Airman
            new BadgeDefinition { Id = "flawless_execution", Title = "Flawless Execution", Description = "Zero delay, zero penalties, perfect touchdown, objectives met.", Tier = AchievementTier.CheckAirman, Icon = "workspace_premium", ColorClass = "text-purple-400" },
            new BadgeDefinition { Id = "through_storm", Title = "Through the Storm", Description = "Landed with crosswind > 20 knots without passenger complaints.", Tier = AchievementTier.CheckAirman, Icon = "storm", ColorClass = "text-purple-400" },
            new BadgeDefinition { Id = "feather_touch", Title = "Feather Touch", Description = "Landed between -10 fpm and -50 fpm. Absolute precision.", Tier = AchievementTier.CheckAirman, Icon = "airline_seat_flat", ColorClass = "text-purple-400" },
            new BadgeDefinition { Id = "iron_bladder", Title = "Iron Bladder", Description = "Logged over 10 hours of block time in a single flight.", Tier = AchievementTier.CheckAirman, Icon = "local_cafe", ColorClass = "text-purple-400" },
            new BadgeDefinition { Id = "airmanship_master", Title = "Airmanship Master", Description = "Achieved a legendary SuperScore of 1200+.", Tier = AchievementTier.CheckAirman, Icon = "rocket_launch", ColorClass = "text-purple-400" },

            // TIER SECRET : Dishonorable
            new BadgeDefinition { Id = "spine_crusher", Title = "Spine Crusher", Description = "Slammed the aircraft down at -600 fpm or worse.", Tier = AchievementTier.Dishonorable, Icon = "personal_injury", ColorClass = "text-red-500" },
            new BadgeDefinition { Id = "no_coffee", Title = "Coffee Machine is Broken", Description = "Skipped catering resulting in high passenger dissatisfaction.", Tier = AchievementTier.Dishonorable, Icon = "no_drinks", ColorClass = "text-red-500" },
            new BadgeDefinition { Id = "pitch_black", Title = "Pitch Black", Description = "Landed at night without Landing Lights.", Tier = AchievementTier.Dishonorable, Icon = "dark_mode", ColorClass = "text-red-500" }
        };

        public List<BadgeDefinition> EvaluateFlightEnd(
            PilotProfile profile, 
            int flightSuperScore, 
            int flightSafetyPoints,
            long flightDelaySec, 
            int flightManualTimeMins, 
            double touchdownFpm, 
            double crosswindKts, 
            bool flightHasSafetyInfraction,
            bool skippedAnyGroundOps,
            bool forgotCatering,
            int comfortPoints,
            bool landingLightsOffAtNight,
            bool isGoAround,
            int blockTimeMins,
            bool allObjectivesMet)
        {
            var newUnlocks = new List<BadgeDefinition>();

            void Unlock(string id)
            {
                if (!profile.UnlockedAchievements.Contains(id))
                {
                    profile.UnlockedAchievements.Add(id);
                    var badge = AllBadges.FirstOrDefault(b => b.Id == id);
                    if (badge != null) newUnlocks.Add(badge);
                }
            }

            // TIER 1
            if (profile.TotalFlights >= 1) Unlock("first_entry");
            if (touchdownFpm < -10 && touchdownFpm > -150) Unlock("butter_bread");
            if (flightDelaySec <= 0) Unlock("swiss_watch");
            if (!skippedAnyGroundOps) Unlock("by_the_book");

            // TIER 2
            if (profile.TotalFlights >= 50) Unlock("frequent_flyer");
            if (flightManualTimeMins >= 10) Unlock("hand_of_god");
            if (flightSuperScore >= 1000) Unlock("company_man");
            
            // Note: For Safe and Sound, we'd need a streak counter. For now, if profile infractions is 0 and flights > 10.
            if (profile.TotalFlights >= 10 && profile.SafetyInfractions == 0) Unlock("safe_and_sound");
            
            if (isGoAround) Unlock("go_around_flaps3");

            // TIER 3
            if (flightDelaySec <= 0 && flightSafetyPoints == 1000 && comfortPoints > 0 && allObjectivesMet && touchdownFpm < 0 && touchdownFpm > -200)
                Unlock("flawless_execution");
                
            if (crosswindKts >= 20 && comfortPoints > 0) Unlock("through_storm");
            if (touchdownFpm <= -10 && touchdownFpm >= -50) Unlock("feather_touch");
            if (blockTimeMins >= 600) Unlock("iron_bladder");
            if (flightSuperScore >= 1200) Unlock("airmanship_master");

            // DISHONORABLE
            if (touchdownFpm <= -600) Unlock("spine_crusher");
            if (forgotCatering && comfortPoints < 0) Unlock("no_coffee");
            if (landingLightsOffAtNight) Unlock("pitch_black");

            return newUnlocks;
        }
    }
}

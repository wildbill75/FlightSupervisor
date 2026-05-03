using System;
using System.Collections.Generic;
using System.Linq;
using FlightSupervisor.UI.Models;

namespace FlightSupervisor.UI.Services
{
    public enum AchievementCategory
    {
        CareerMilestones,
        Airmanship,
        SafetyAndCompliance,
        AdverseConditions,
        HallOfShame
    }

    public class BadgeDefinition
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public AchievementCategory Category { get; set; }
        public string Icon { get; set; }
        public string ColorClass { get; set; }
    }

    public class AchievementEngine
    {
        public static List<BadgeDefinition> AllBadges = new List<BadgeDefinition>
        {
            // CAREER MILESTONES
            new BadgeDefinition { Id = "first_entry", Title = "First Entry", Description = "Completed your first flight log.", Category = AchievementCategory.CareerMilestones, Icon = "menu_book", ColorClass = "text-sky-400" },
            new BadgeDefinition { Id = "frequent_flyer", Title = "Frequent Flyer", Description = "Logged 50 successful flights.", Category = AchievementCategory.CareerMilestones, Icon = "military_tech", ColorClass = "text-amber-400" },
            new BadgeDefinition { Id = "centurion", Title = "Centurion", Description = "Completed 100 flights.", Category = AchievementCategory.CareerMilestones, Icon = "workspace_premium", ColorClass = "text-purple-400" },
            new BadgeDefinition { Id = "globe_trotter", Title = "Globe Trotter", Description = "Accumulated over 100 hours of total flight time.", Category = AchievementCategory.CareerMilestones, Icon = "public", ColorClass = "text-purple-400" },
            new BadgeDefinition { Id = "iron_bladder", Title = "Iron Bladder", Description = "Logged over 10 hours of block time in a single flight.", Category = AchievementCategory.CareerMilestones, Icon = "local_cafe", ColorClass = "text-purple-400" },

            // AIRMANSHIP
            new BadgeDefinition { Id = "butter_bread", Title = "Butter the Bread", Description = "Landed smoother than -150 fpm.", Category = AchievementCategory.Airmanship, Icon = "flight_land", ColorClass = "text-sky-400" },
            new BadgeDefinition { Id = "feather_touch", Title = "Feather Touch", Description = "Landed between -10 fpm and -50 fpm. Absolute precision.", Category = AchievementCategory.Airmanship, Icon = "airline_seat_flat", ColorClass = "text-purple-400" },
            new BadgeDefinition { Id = "hand_of_god", Title = "The Hand of God", Description = "Over 10 minutes of manual flying in a single flight.", Category = AchievementCategory.Airmanship, Icon = "front_hand", ColorClass = "text-amber-400" },
            new BadgeDefinition { Id = "go_around_flaps3", Title = "Go-Around, Flaps 3", Description = "Successfully executed a Go-Around.", Category = AchievementCategory.Airmanship, Icon = "autorenew", ColorClass = "text-amber-400" },
            new BadgeDefinition { Id = "company_man", Title = "Company Man", Description = "Earned a SuperScore over 1000 on a single sector.", Category = AchievementCategory.Airmanship, Icon = "work", ColorClass = "text-amber-400" },
            new BadgeDefinition { Id = "airmanship_master", Title = "Airmanship Master", Description = "Achieved a legendary SuperScore of 1200+.", Category = AchievementCategory.Airmanship, Icon = "rocket_launch", ColorClass = "text-purple-400" },

            // SAFETY AND COMPLIANCE
            new BadgeDefinition { Id = "swiss_watch", Title = "Swiss Watch", Description = "Arrived on or before scheduled time.", Category = AchievementCategory.SafetyAndCompliance, Icon = "schedule", ColorClass = "text-sky-400" },
            new BadgeDefinition { Id = "by_the_book", Title = "By the Book", Description = "Completed all ground operations without rushing.", Category = AchievementCategory.SafetyAndCompliance, Icon = "checklist_rtl", ColorClass = "text-sky-400" },
            new BadgeDefinition { Id = "safe_and_sound", Title = "Safe and Sound", Description = "Completed 10 flights in a row with zero safety infractions.", Category = AchievementCategory.SafetyAndCompliance, Icon = "verified_user", ColorClass = "text-amber-400" },
            new BadgeDefinition { Id = "flawless_execution", Title = "Flawless Execution", Description = "Zero delay, zero penalties, perfect touchdown, objectives met.", Category = AchievementCategory.SafetyAndCompliance, Icon = "stars", ColorClass = "text-purple-400" },
            new BadgeDefinition { Id = "passengers_favorite", Title = "Passenger's Favorite", Description = "Maintained a positive comfort rating with zero infractions.", Category = AchievementCategory.SafetyAndCompliance, Icon = "favorite", ColorClass = "text-amber-400" },

            // ADVERSE CONDITIONS
            new BadgeDefinition { Id = "through_storm", Title = "Through the Storm", Description = "Landed with crosswind > 20 knots without passenger complaints.", Category = AchievementCategory.AdverseConditions, Icon = "storm", ColorClass = "text-purple-400" },
            new BadgeDefinition { Id = "night_owl", Title = "Night Owl", Description = "Completed a safe landing at night.", Category = AchievementCategory.AdverseConditions, Icon = "bedtime", ColorClass = "text-purple-400" },

            // HALL OF SHAME
            new BadgeDefinition { Id = "spine_crusher", Title = "Spine Crusher", Description = "Slammed the aircraft down at -600 fpm or worse.", Category = AchievementCategory.HallOfShame, Icon = "personal_injury", ColorClass = "text-red-500" },
            new BadgeDefinition { Id = "no_coffee", Title = "Coffee Machine is Broken", Description = "Skipped catering resulting in high passenger dissatisfaction.", Category = AchievementCategory.HallOfShame, Icon = "no_drinks", ColorClass = "text-red-500" },
            new BadgeDefinition { Id = "pitch_black", Title = "Pitch Black", Description = "Landed at night without Landing Lights.", Category = AchievementCategory.HallOfShame, Icon = "dark_mode", ColorClass = "text-red-500" },
            new BadgeDefinition { Id = "schedule_buster", Title = "Schedule Buster", Description = "Landed with more than 60 minutes of delay.", Category = AchievementCategory.HallOfShame, Icon = "alarm_off", ColorClass = "text-red-500" }
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
            bool allObjectivesMet,
            bool isNight)
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

            // CAREER MILESTONES
            if (profile.TotalFlights >= 1) Unlock("first_entry");
            if (profile.TotalFlights >= 50) Unlock("frequent_flyer");
            if (profile.TotalFlights >= 100) Unlock("centurion");
            if (profile.TotalBlockTimeMinutes >= 6000) Unlock("globe_trotter"); // 100 hours
            if (blockTimeMins >= 600) Unlock("iron_bladder");

            // AIRMANSHIP
            if (touchdownFpm < -10 && touchdownFpm > -150) Unlock("butter_bread");
            if (touchdownFpm <= -10 && touchdownFpm >= -50) Unlock("feather_touch");
            if (flightManualTimeMins >= 10) Unlock("hand_of_god");
            if (isGoAround) Unlock("go_around_flaps3");
            if (flightSuperScore >= 1000) Unlock("company_man");
            if (flightSuperScore >= 1200) Unlock("airmanship_master");

            // SAFETY AND COMPLIANCE
            if (flightDelaySec <= 0) Unlock("swiss_watch");
            if (!skippedAnyGroundOps) Unlock("by_the_book");
            if (profile.TotalFlights >= 10 && profile.SafetyInfractions == 0) Unlock("safe_and_sound");
            if (flightDelaySec <= 0 && flightSafetyPoints == 1000 && comfortPoints > 0 && allObjectivesMet && touchdownFpm < 0 && touchdownFpm > -200)
                Unlock("flawless_execution");
            if (comfortPoints > 0 && !flightHasSafetyInfraction) Unlock("passengers_favorite");

            // ADVERSE CONDITIONS
            if (crosswindKts >= 20 && comfortPoints > 0) Unlock("through_storm");
            if (isNight && !landingLightsOffAtNight) Unlock("night_owl");

            // HALL OF SHAME
            if (touchdownFpm <= -600) Unlock("spine_crusher");
            if (forgotCatering && comfortPoints < 0) Unlock("no_coffee");
            if (landingLightsOffAtNight) Unlock("pitch_black");
            if (flightDelaySec > 3600) Unlock("schedule_buster");

            return newUnlocks;
        }
    }
}

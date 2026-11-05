using System;

namespace DigitDuelist
{
    /// <summary>
    /// Age-appropriate mathematical skill tiers for children.
    /// Adjusts operand bounds, operation complexity, and time pressure.
    /// </summary>
    public enum AgeDifficultyPreset
    {
        Novice,     // Ages 5–7: Single-digit Addition & Subtraction (1–10)
        Adventurer, // Ages 8–9: Addition/Subtraction up to 50, Multiplication tables 1–5
        Master      // Ages 10+: Full times tables up to 12, Division, MDAS
    }

    public static class AgeDifficultyExtensions
    {
        public static string GetDisplayName(this AgeDifficultyPreset preset)
        {
            switch (preset)
            {
                case AgeDifficultyPreset.Novice:
                    return "🌱 Novice (Ages 5–7)";
                case AgeDifficultyPreset.Adventurer:
                    return "⚔️ Adventurer (Ages 8–9)";
                case AgeDifficultyPreset.Master:
                    return "👑 Master Duelist (Ages 10+)";
                default:
                    return preset.ToString();
            }
        }

        public static string GetDescription(this AgeDifficultyPreset preset)
        {
            switch (preset)
            {
                case AgeDifficultyPreset.Novice:
                    return "Gentle math with numbers 1 to 10. Perfect for building early confidence!";
                case AgeDifficultyPreset.Adventurer:
                    return "Two-digit sums up to 50 and starter multiplication tables (1 to 5).";
                case AgeDifficultyPreset.Master:
                    return "Complete 12×12 tables, division quests, and mixed arithmetic challenges!";
                default:
                    return "";
            }
        }
    }
}

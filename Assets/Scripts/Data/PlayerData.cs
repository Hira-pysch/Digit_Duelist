using System;
using UnityEngine;

namespace DigitDuelist
{
    /// <summary>
    /// Holds player progression data, stats, and run status.
    /// Serialized directly to JSON by SaveManager.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        public int currentLevel = 1;
        public int playerHealth = 145;
        public int maxHealth = 145;
        public int attackPower = 25;
        public int defense = 0;
        public int xp = 0;
        public int xpToNextLevel = 100;
        public int score = 0;
        public int highestStreak = 0;
        public MathOperation chosenMode = MathOperation.Addition;

        public PlayerData()
        {
            ResetToDefaults();
        }

        public void ResetToDefaults()
        {
            currentLevel = 1;
            maxHealth = 145; // Constant baseline Max Health
            playerHealth = maxHealth;
            attackPower = 25;
            defense = 0;
            xp = 0;
            xpToNextLevel = 100;
            score = 0;
            highestStreak = 0;
            chosenMode = MathOperation.Addition;
        }

        public void ApplyAttackBoost(int amount = 10)
        {
            attackPower += amount;
        }

        public void ApplyHealthBoost(int amount = 30)
        {
            maxHealth += amount;
            playerHealth = maxHealth; // Heal to full on upgrade
        }

        public void ApplyDefenseBoost(int amount = 5)
        {
            defense += amount;
        }

        /// <summary>
        /// Adds XP and checks for level up thresholds.
        /// </summary>
        public bool AddXP(int amount)
        {
            xp += amount;
            bool leveledUp = false;

            while (xp >= xpToNextLevel)
            {
                xp -= xpToNextLevel;
                LevelUp();
                leveledUp = true;
            }

            return leveledUp;
        }

        /// <summary>
        /// Increases level and restores player health to current Max Health.
        /// Asymmetric scaling: Max Health does NOT grow automatically on level up;
        /// it ONLY increases when the player selects the Health Boost upgrade.
        /// </summary>
        public void LevelUp()
        {
            currentLevel++;
            playerHealth = maxHealth; // Full heal to current max health
            xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.25f);
        }

        public void ResetHealth()
        {
            playerHealth = maxHealth;
        }

        public PlayerData Clone()
        {
            return (PlayerData)this.MemberwiseClone();
        }
    }
}

using System;
using UnityEngine;

namespace DigitDuelist
{
    public enum UpgradeBuffType
    {
        AttackBoost,
        HealthBoost,
        DefenseBoost
    }

    [Serializable]
    public struct UpgradeBuffOption
    {
        public UpgradeBuffType type;
        public string title;
        public string description;
        public string bonusValueText;
        public Color themeColor;
    }

    /// <summary>
    /// Coordinates the 5-Level Milestone Upgrade System.
    /// Every 5 levels, pauses level transition and prompts the player with 3 permanent stat buffs:
    /// - Attack Boost: Increases damage dealt on correct answers
    /// - Health Boost: Increases max HP and heals the player
    /// - Defense Boost: Reduces incoming damage taken on errors/timeouts
    /// </summary>
    public class LevelUpgradeManager : MonoBehaviour
    {
        public static LevelUpgradeManager Instance { get; private set; }

        [Header("Upgrade Values")]
        [SerializeField] private int attackBoostAmount = 10;
        [SerializeField] private int healthBoostAmount = 30;
        [SerializeField] private int defenseBoostAmount = 5;

        [Header("Dependencies")]
        [SerializeField] private UpgradeSelectionUI upgradeUI;

        // Events
        public event Action<UpgradeBuffOption[]> OnUpgradePrompted;
        public event Action<UpgradeBuffType> OnUpgradeApplied;

        public int AttackBoostAmount => attackBoostAmount;
        public int HealthBoostAmount => healthBoostAmount;
        public int DefenseBoostAmount => defenseBoostAmount;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (upgradeUI == null)
            {
                upgradeUI = UpgradeSelectionUI.EnsureInstance();
            }
        }

        private void Update()
        {
            // Debug test shortcut: Press 'U' during gameplay to immediately test the 3-choice upgrade modal
            if (GameInput.GetKeyDown(KeyCode.U))
            {
                PromptUpgradeSelection(null);
            }
        }

        /// <summary>
        /// Checks whether the completed level is a 5-level milestone (Level 5, 10, 15, etc.).
        /// </summary>
        public bool IsMilestoneLevel(int completedLevel)
        {
            return completedLevel > 0 && (completedLevel % 5 == 0);
        }

        /// <summary>
        /// Builds options and triggers the upgrade UI modal.
        /// </summary>
        public void PromptUpgradeSelection(Action onCompleteCallback)
        {
            PlayerData data = GameManager.Instance != null ? GameManager.Instance.CurrentPlayerData : null;
            int currentAtk = data != null ? data.attackPower : 25;
            int currentHp = data != null ? data.maxHealth : 100;
            int currentDef = data != null ? data.defense : 0;

            UpgradeBuffOption[] options = new UpgradeBuffOption[]
            {
                new UpgradeBuffOption
                {
                    type = UpgradeBuffType.AttackBoost,
                    title = "⚔️ ATTACK BOOST",
                    description = $"Increase strike damage on correct answers.\n<size=80%>(Current: {currentAtk} → <b>{currentAtk + attackBoostAmount}</b>)</size>",
                    bonusValueText = $"+{attackBoostAmount} ATK",
                    themeColor = new Color(0.95f, 0.35f, 0.25f, 1f)
                },
                new UpgradeBuffOption
                {
                    type = UpgradeBuffType.HealthBoost,
                    title = "💖 HEALTH BOOST",
                    description = $"Expand max vitality and restore full health.\n<size=80%>(Current: {currentHp} → <b>{currentHp + healthBoostAmount}</b>)</size>",
                    bonusValueText = $"+{healthBoostAmount} HP",
                    themeColor = new Color(0.25f, 0.85f, 0.45f, 1f)
                },
                new UpgradeBuffOption
                {
                    type = UpgradeBuffType.DefenseBoost,
                    title = "🛡️ DEFENSE BOOST",
                    description = $"Fortify armor to absorb incoming mistakes.\n<size=80%>(Current: {currentDef} → <b>{currentDef + defenseBoostAmount}</b>)</size>",
                    bonusValueText = $"+{defenseBoostAmount} DEF",
                    themeColor = new Color(0.25f, 0.65f, 1f, 1f)
                }
            };

            OnUpgradePrompted?.Invoke(options);

            if (upgradeUI != null)
            {
                upgradeUI.DisplayUpgradePrompt(options, (chosenType) =>
                {
                    ApplyUpgrade(chosenType);
                    onCompleteCallback?.Invoke();
                });
            }
            else
            {
                // Fallback: apply default and continue
                ApplyUpgrade(UpgradeBuffType.AttackBoost);
                onCompleteCallback?.Invoke();
            }
        }

        /// <summary>
        /// Applies the selected permanent stat buff to player data and saves progression.
        /// </summary>
        public void ApplyUpgrade(UpgradeBuffType upgradeType)
        {
            PlayerData player = GameManager.Instance != null ? GameManager.Instance.CurrentPlayerData : null;
            if (player == null) return;

            string buffName = "";

            switch (upgradeType)
            {
                case UpgradeBuffType.AttackBoost:
                    player.ApplyAttackBoost(attackBoostAmount);
                    buffName = $"+{attackBoostAmount} Attack Power";
                    break;

                case UpgradeBuffType.HealthBoost:
                    player.ApplyHealthBoost(healthBoostAmount);
                    buffName = $"+{healthBoostAmount} Max Health";
                    break;

                case UpgradeBuffType.DefenseBoost:
                    player.ApplyDefenseBoost(defenseBoostAmount);
                    buffName = $"+{defenseBoostAmount} Armor Defense";
                    break;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveCurrentGame();
                GameManager.Instance.Notify($"🌟 Permanent Upgrade Acquired: {buffName}!");
            }

            if (KidAudioManager.Instance != null)
            {
                KidAudioManager.Instance.PlayStreakFanfare();
            }

            OnUpgradeApplied?.Invoke(upgradeType);
        }
    }
}

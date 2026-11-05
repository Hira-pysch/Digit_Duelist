using System;
using System.Collections;
using UnityEngine;

namespace DigitDuelist
{
    /// <summary>
    /// Manages the in-combat math duel loop: question delivery, answer verification,
    /// enemy health scaling, damage exchange, character combat animations, and win/loss triggers.
    /// </summary>
    public class DuelController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private MathGenerator mathGenerator;

        [Header("Visual Characters")]
        [SerializeField] private CharacterDuelVisuals playerVisuals;
        [SerializeField] private CharacterDuelVisuals enemyVisuals;

        [Header("Asymmetric Enemy Scaling")]
        [SerializeField] private int enemyBaseHealth = 100;
        [SerializeField] private int enemyHealthPerLevel = 25;
        [SerializeField] private float bossHealthMultiplier = 1.75f;

        [Header("Enemy Stats")]
        [SerializeField] private string currentEnemyName = "Knight Challenger";
        [SerializeField] private int enemyMaxHealth = 100;
        [SerializeField] private int enemyCurrentHealth = 100;
        [SerializeField] private int enemyAttackPower = 15;

        [Header("Turn Timer Settings")]
        [SerializeField] private bool useTurnTimer = true;
        [SerializeField] private float timePerTurn = 12f;
        private float currentTurnTimeRemaining;
        private bool isTurnActive = false;

        [Header("Current Question")]
        private MathQuestion currentQuestion;
        private int currentStreak = 0;

        // Events for UI and Sound systems
        public event Action<MathQuestion> OnNewQuestionGenerated;
        public event Action<bool, int> OnAnswerEvaluated; // isCorrect, correctAnswer
        public event Action<int, int> OnPlayerHealthChanged; // current, max
        public event Action<int, int> OnEnemyHealthChanged;  // current, max
        public event Action<float, float> OnTimerTick;       // remaining, max
        public event Action<int> OnStreakChanged;

        public MathQuestion CurrentQuestion => currentQuestion;
        public int EnemyCurrentHealth => enemyCurrentHealth;
        public int EnemyMaxHealth => enemyMaxHealth;
        public string CurrentEnemyName => currentEnemyName;
        public int CurrentStreak => currentStreak;
        public CharacterDuelVisuals PlayerVisuals => playerVisuals;
        public CharacterDuelVisuals EnemyVisuals => enemyVisuals;

        private void Awake()
        {
            if (mathGenerator == null)
            {
                mathGenerator = GetComponent<MathGenerator>() ?? FindFirstObjectByType<MathGenerator>();
            }

            FindCharacterVisuals();
        }

        public void FindCharacterVisuals()
        {
            if (playerVisuals == null || enemyVisuals == null)
            {
                CharacterDuelVisuals[] visuals = FindObjectsByType<CharacterDuelVisuals>(FindObjectsSortMode.None);
                foreach (var v in visuals)
                {
                    if (v.IsPlayer && playerVisuals == null) playerVisuals = v;
                    else if (!v.IsPlayer && enemyVisuals == null) enemyVisuals = v;
                }
            }
        }

        private void Update()
        {
            // If Zen Mode is active, turn timer is completely disabled
            bool zenMode = GameModeManager.Instance != null && GameModeManager.Instance.IsZenMode;
            if (!isTurnActive || !useTurnTimer || zenMode) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Gameplay) return;

            // Pause countdown if Time Freeze spell is currently active
            if (AssistSpellsController.Instance != null && AssistSpellsController.Instance.IsTimerFrozen)
            {
                return;
            }

            currentTurnTimeRemaining -= Time.deltaTime;
            OnTimerTick?.Invoke(Mathf.Max(0, currentTurnTimeRemaining), timePerTurn);

            if (currentTurnTimeRemaining <= 0f)
            {
                isTurnActive = false;
                HandleTimeout();
            }
        }

        /// <summary>
        /// Initializes combat for the current level and player stats.
        /// </summary>
        public void StartDuel(PlayerData playerData)
        {
            FindCharacterVisuals();

            int level = playerData != null ? playerData.currentLevel : 1;
            bool isBoss = (level > 0 && level % 5 == 0);

            // Asymmetric Enemy Scaling Formula: BaseHP + ((currentLevel - 1) * healthPerLevel)
            int scaledEnemyHp = enemyBaseHealth + ((level - 1) * enemyHealthPerLevel);

            if (isBoss)
            {
                // Boss receives an extra health multiplier on top of the level scaling
                enemyMaxHealth = Mathf.RoundToInt(scaledEnemyHp * bossHealthMultiplier);
                enemyAttackPower = Mathf.RoundToInt((18 + (level * 3)) * 1.3f);
                currentEnemyName = $"👑 BOSS: {GetBossNameForLevel(level)}";
            }
            else
            {
                enemyMaxHealth = scaledEnemyHp;
                enemyAttackPower = 15 + ((level - 1) * 3);
                currentEnemyName = GetEnemyNameForLevel(level);
            }

            enemyCurrentHealth = enemyMaxHealth;
            currentStreak = 0;

            if (playerData != null)
            {
                playerData.ResetHealth();
                OnPlayerHealthChanged?.Invoke(playerData.playerHealth, playerData.maxHealth);
            }

            OnEnemyHealthChanged?.Invoke(enemyCurrentHealth, enemyMaxHealth);
            OnStreakChanged?.Invoke(currentStreak);

            if (playerVisuals != null) playerVisuals.ResetStance();
            if (enemyVisuals != null) enemyVisuals.ResetStance();

            NextTurn(playerData);
        }

        /// <summary>
        /// Generates the next question and starts turn timer.
        /// </summary>
        public void NextTurn(PlayerData playerData)
        {
            int level = playerData != null ? playerData.currentLevel : 1;
            MathOperation mode = playerData != null ? playerData.chosenMode : MathOperation.Addition;

            if (mathGenerator == null)
            {
                mathGenerator = FindFirstObjectByType<MathGenerator>();
            }

            currentQuestion = mathGenerator.GenerateQuestion(mode, level);
            currentTurnTimeRemaining = timePerTurn;
            isTurnActive = true;

            // Trigger Boss Cognitive Disruption if Level 5 Boss encounter
            bool isBoss = (level > 0 && level % 5 == 0);
            if (BossChoiceFlipper.Instance != null)
            {
                BossChoiceFlipper.Instance.OnNewQuestionStarted(isBoss);
            }

            OnNewQuestionGenerated?.Invoke(currentQuestion);
        }

        /// <summary>
        /// Submits an answer choice and resolves damage & progression with visual choreography.
        /// </summary>
        public void SubmitAnswer(int chosenAnswer, PlayerData playerData)
        {
            if (!isTurnActive) return;
            isTurnActive = false;

            bool isCorrect = (chosenAnswer == currentQuestion.correctAnswer);
            OnAnswerEvaluated?.Invoke(isCorrect, currentQuestion.correctAnswer);

            StartCoroutine(ResolveTurnRoutine(isCorrect, playerData));
        }

        private void HandleTimeout()
        {
            OnAnswerEvaluated?.Invoke(false, currentQuestion.correctAnswer);
            PlayerData data = GameManager.Instance != null ? GameManager.Instance.CurrentPlayerData : null;
            StartCoroutine(ResolveTurnRoutine(false, data));
        }

        private IEnumerator ResolveTurnRoutine(bool isCorrect, PlayerData playerData)
        {
            FindCharacterVisuals();

            Vector3 playerPos = playerVisuals != null ? playerVisuals.transform.position : new Vector3(-3f, 0f, 0f);
            Vector3 enemyPos = enemyVisuals != null ? enemyVisuals.transform.position : new Vector3(3f, 0f, 0f);

            if (isCorrect)
            {
                currentStreak++;
                if (playerData != null && currentStreak > playerData.highestStreak)
                {
                    playerData.highestStreak = currentStreak;
                }

                int damage = playerData != null ? playerData.attackPower : 25;
                if (currentStreak > 2) damage += (currentStreak * 5); // Streak critical boost

                // 1. Player attacks enemy with dynamic lunge
                Vector3 attackDir = (enemyPos - playerPos).normalized;
                if (attackDir == Vector3.zero) attackDir = Vector3.right;

                bool impactHappened = false;
                if (playerVisuals != null)
                {
                    playerVisuals.PlayAttack(attackDir, () =>
                    {
                        impactHappened = true;
                        // On Impact:
                        enemyCurrentHealth = Mathf.Max(0, enemyCurrentHealth - damage);
                        OnEnemyHealthChanged?.Invoke(enemyCurrentHealth, enemyMaxHealth);

                        if (enemyVisuals != null) enemyVisuals.PlayHitReaction(attackDir);

                        // Floating damage number & combat spark bursts
                        if (CombatFXController.Instance != null)
                        {
                            CombatFXController.Instance.ShakeCamera(0.28f, 0.22f);
                            bool isCrit = currentStreak >= 3;
                            if (isCrit)
                            {
                                CombatFXController.Instance.TriggerActionCamZoom(0.32f, 38f);
                            }

                            CombatFXController.Instance.SpawnHitSparks(enemyPos + Vector3.up * 1f, isCrit ? new Color(1f, 0.88f, 0.2f) : new Color(0.3f, 0.85f, 1f), isCrit ? 22 : 14);

                            string dmgText = isCrit ? $"CRITICAL! -{damage}" : $"-{damage}";
                            CombatFXController.Instance.SpawnFloatingText(enemyPos, dmgText, isCrit ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.35f, 0.35f), 5f, isCrit);

                            if (currentStreak > 1)
                            {
                                CombatFXController.Instance.SpawnFloatingText(playerPos + Vector3.up * 0.6f, $"🔥 STREAK x{currentStreak}!", new Color(1f, 0.6f, 0.1f), 4.2f);
                            }
                        }
                    });
                }
                else
                {
                    enemyCurrentHealth = Mathf.Max(0, enemyCurrentHealth - damage);
                    OnEnemyHealthChanged?.Invoke(enemyCurrentHealth, enemyMaxHealth);
                }

                // Wait for animation to finish
                yield return new WaitForSeconds(0.65f);

                // Add score & XP
                if (playerData != null)
                {
                    int xpReward = 15 + (playerData.currentLevel * 5) + (currentStreak * 2);
                    int scoreReward = 100 * (1 + (currentStreak / 2));
                    playerData.score += scoreReward;
                    playerData.AddXP(xpReward);

                    if (CombatFXController.Instance != null)
                    {
                        CombatFXController.Instance.SpawnFloatingText(playerPos, $"+{xpReward} XP", new Color(0.3f, 0.85f, 1f), 4f);
                    }
                }

                OnStreakChanged?.Invoke(currentStreak);

                // Check for milestone skin unlocks
                if (SkinCustomizationManager.Instance != null && playerData != null)
                {
                    SkinCustomizationManager.Instance.CheckUnlocks(playerData.currentLevel, currentStreak);
                }

                // Check if enemy defeated
                if (enemyCurrentHealth <= 0)
                {
                    if (enemyVisuals != null) enemyVisuals.PlayDefeat();
                    if (playerVisuals != null) playerVisuals.PlayVictory();

                    if (CombatFXController.Instance != null)
                    {
                        CombatFXController.Instance.SpawnFloatingText(enemyPos, "VICTORY! 🌟", new Color(1f, 0.85f, 0.2f), 6f, true);
                    }

                    yield return new WaitForSeconds(0.8f);
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.OnLevelComplete();
                    }
                    yield break;
                }
            }
            else
            {
                currentStreak = 0;
                OnStreakChanged?.Invoke(currentStreak);

                // Check if Bubble Shield absorbs the mistake
                bool shieldAbsorbed = (AssistSpellsController.Instance != null && AssistSpellsController.Instance.TryAbsorbDamageWithShield());

                if (shieldAbsorbed)
                {
                    // Shield completely absorbed the mistake! No health lost!
                    yield return new WaitForSeconds(0.45f);
                }
                else
                {
                    // 2. Enemy attacks player
                    Vector3 enemyAttackDir = (playerPos - enemyPos).normalized;
                    if (enemyAttackDir == Vector3.zero) enemyAttackDir = Vector3.left;

                    if (enemyVisuals != null)
                    {
                        enemyVisuals.PlayAttack(enemyAttackDir, () =>
                        {
                            int playerDef = playerData != null ? playerData.defense : 0;
                            int netDamage = Mathf.Max(5, enemyAttackPower - playerDef);

                            if (playerData != null)
                            {
                                playerData.playerHealth = Mathf.Max(0, playerData.playerHealth - netDamage);
                                OnPlayerHealthChanged?.Invoke(playerData.playerHealth, playerData.maxHealth);
                            }

                            if (playerVisuals != null) playerVisuals.PlayHitReaction(enemyAttackDir);

                            if (CombatFXController.Instance != null)
                            {
                                CombatFXController.Instance.ShakeCamera(0.25f, 0.2f);
                                CombatFXController.Instance.SpawnHitSparks(playerPos + Vector3.up * 1f, new Color(1f, 0.35f, 0.2f), 14);
                                CombatFXController.Instance.SpawnFloatingText(playerPos, $"-{netDamage}", new Color(1f, 0.45f, 0.45f), 4.8f);

                                if (playerDef > 0)
                                {
                                    CombatFXController.Instance.SpawnFloatingText(playerPos + Vector3.up * 0.7f, $"🛡️ -{playerDef} DEFENSE", new Color(0.35f, 0.75f, 1f), 3.8f);
                                }
                            }
                        });
                    }
                    else if (playerData != null)
                    {
                        int playerDef = playerData.defense;
                        int netDamage = Mathf.Max(5, enemyAttackPower - playerDef);
                        playerData.playerHealth = Mathf.Max(0, playerData.playerHealth - netDamage);
                        OnPlayerHealthChanged?.Invoke(playerData.playerHealth, playerData.maxHealth);
                    }

                    yield return new WaitForSeconds(0.65f);

                    // Check if player needs a practice break
                    if (playerData != null && playerData.playerHealth <= 0)
                    {
                        if (BossChoiceFlipper.Instance != null) BossChoiceFlipper.Instance.ResetButtonsToDefaultHomePositions();
                        if (playerVisuals != null) playerVisuals.PlayDefeat();
                        if (enemyVisuals != null) enemyVisuals.PlayVictory();

                        if (CombatFXController.Instance != null)
                        {
                            CombatFXController.Instance.SpawnFloatingText(playerPos, "PRACTICE BREAK! 🌟", new Color(1f, 0.85f, 0.3f), 5.5f, true);
                        }

                        yield return new WaitForSeconds(0.8f);
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.OnGameOver();
                        }
                        yield break;
                    }
                }
            }

            // Next question in duel
            yield return new WaitForSeconds(0.35f);
            NextTurn(playerData);
        }

        private string GetEnemyNameForLevel(int level)
        {
            string[] titles = {
                "Squire Calculator", "Apprentice Arithmetician", "Knight of the Plus",
                "Division Duelist", "Baron of Algebra", "Grand Math Sorcerer", "Archmage Matrix"
            };
            int index = Mathf.Clamp(level - 1, 0, titles.Length - 1);
            return $"{titles[index]} (Lv. {level})";
        }

        private string GetBossNameForLevel(int level)
        {
            string[] bossTitles = {
                "Gargoyle of Arithmetic",
                "Archmage Calculus",
                "Titan of Multiplication",
                "Dragon of Division",
                "Grand Chancellor Matrix"
            };
            int bossIndex = Mathf.Clamp((level / 5) - 1, 0, bossTitles.Length - 1);
            return $"{bossTitles[bossIndex]} (Lv. {level})";
        }
    }
}

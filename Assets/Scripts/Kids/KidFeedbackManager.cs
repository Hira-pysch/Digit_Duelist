using System.Collections;
using UnityEngine;
using TMPro;

namespace DigitDuelist
{
    /// <summary>
    /// Cultivates a positive Growth Mindset for kids:
    /// 1. Reframes failure with encouraging, empowering messaging.
    /// 2. Manages playful cartoon dizzy states with orbiting stars instead of violent knockouts.
    /// 3. Hooks whimsical audio and opens visual math breakdown hints on tricky problems.
    /// </summary>
    public class KidFeedbackManager : MonoBehaviour
    {
        public static KidFeedbackManager Instance { get; private set; }

        [Header("Subsystem References")]
        [SerializeField] private DuelController duelController;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private VisualMathHintCard hintCard;

        [Header("Encouraging Messages (Growth Mindset)")]
        private readonly string[] tryAgainEncouragements = new string[]
        {
            "🌟 Nice try! Every mistake makes your brain stronger!",
            "🌱 Almost there! Practice makes math magic happen!",
            "🛡️ Great effort, hero! Let's regroup and solve it!",
            "💡 You're learning fast! Let's try another one together!",
            "✨ Mistakes are proof that you are trying! Keep going!"
        };

        private readonly string[] praiseMessages = new string[]
        {
            "🌟 BRILLIANT!",
            "🎉 MATH WIZARD!",
            "⭐ SUPERSTAR!",
            "🔥 UNSTOPPABLE!",
            "💎 MASTERMIND!"
        };

        private OrbitingDizzyStars activeDizzyStars;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            FindDependencies();
        }

        private void OnEnable()
        {
            if (duelController != null)
            {
                duelController.OnAnswerEvaluated += HandleAnswerFeedback;
                duelController.OnStreakChanged += HandleStreakFeedback;
            }

            if (gameManager != null)
            {
                gameManager.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDisable()
        {
            if (duelController != null)
            {
                duelController.OnAnswerEvaluated -= HandleAnswerFeedback;
                duelController.OnStreakChanged -= HandleStreakFeedback;
            }

            if (gameManager != null)
            {
                gameManager.OnStateChanged -= HandleStateChanged;
            }
        }

        private void FindDependencies()
        {
            if (duelController == null) duelController = FindFirstObjectByType<DuelController>();
            if (gameManager == null) gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
            if (hintCard == null) hintCard = FindFirstObjectByType<VisualMathHintCard>();
        }

        private void HandleAnswerFeedback(bool isCorrect, int correctAnswer)
        {
            if (isCorrect)
            {
                // Play joyful harp chime
                if (KidAudioManager.Instance != null) KidAudioManager.Instance.PlayCorrectAnswer();

                // Clear any leftover dizzy stars
                ClearDizzyStars();

                if (hintCard != null) hintCard.HideHint();
            }
            else
            {
                // Play soft cartoon boing instead of loud alarm
                if (KidAudioManager.Instance != null) KidAudioManager.Instance.PlayWrongAnswer();

                // Gentle floating encouragement
                if (CombatFXController.Instance != null && duelController != null && duelController.PlayerVisuals != null)
                {
                    string randomPraise = tryAgainEncouragements[Random.Range(0, tryAgainEncouragements.Length)];
                    CombatFXController.Instance.SpawnFloatingText(
                        duelController.PlayerVisuals.transform.position + Vector3.up * 1.7f,
                        "Nice try! You've got this! ✨",
                        new Color(1f, 0.85f, 0.4f),
                        4f
                    );
                }

                // Show visual math hint card for educational reinforcement
                if (hintCard != null && duelController != null)
                {
                    hintCard.ShowHint(duelController.CurrentQuestion);
                }
            }
        }

        private void HandleStreakFeedback(int streak)
        {
            if (streak >= 3)
            {
                // Trigger fanfare & celebration
                if (KidAudioManager.Instance != null) KidAudioManager.Instance.PlayStreakFanfare();

                string cheer = praiseMessages[Random.Range(0, praiseMessages.Length)];
                if (CombatFXController.Instance != null && duelController != null && duelController.PlayerVisuals != null)
                {
                    CombatFXController.Instance.SpawnFloatingText(
                        duelController.PlayerVisuals.transform.position + Vector3.up * 2.2f,
                        $"{cheer}\n{streak} IN A ROW! 🏆",
                        new Color(1f, 0.9f, 0.2f),
                        5f,
                        true
                    );
                }
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                ReframeGameOverScreen();
                TriggerPlayfulDizzyState();
            }
            else if (state == GameState.Gameplay)
            {
                ClearDizzyStars();
            }
        }

        /// <summary>
        /// Replaces punitive "DEFEAT / FALLEN IN BATTLE" screen texts with growth-mindset encouragement.
        /// </summary>
        private void ReframeGameOverScreen()
        {
            GameObject gameOverPanel = GameObject.Find("GameOver_Panel");
            if (gameOverPanel == null) return;

            // Update title text
            TextMeshProUGUI[] tmps = gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                if (t.text.Contains("DEFEAT") || t.text.Contains("FALLEN"))
                {
                    t.text = "🌟 Practice Break!";
                    t.color = new Color(0.35f, 0.85f, 1f);
                }
                else if (t.text.Contains("Final Score") || t.text.Contains("Score:"))
                {
                    PlayerData data = gameManager != null ? gameManager.CurrentPlayerData : null;
                    int score = data != null ? data.score : 0;
                    t.text = $"Awesome Effort! Points Earned: {score:N0} 🏅";
                }
                else if (t.text.Contains("Level Reached"))
                {
                    t.text = tryAgainEncouragements[Random.Range(0, tryAgainEncouragements.Length)];
                }
            }
        }

        /// <summary>
        /// Triggers a cartoon wobble and spawns orbiting stars over the player's head.
        /// </summary>
        private void TriggerPlayfulDizzyState()
        {
            ClearDizzyStars();

            if (duelController == null || duelController.PlayerVisuals == null) return;

            Transform playerTrans = duelController.PlayerVisuals.transform;

            GameObject dizzyGo = new GameObject("Player_OrbitingDizzyStars");
            dizzyGo.transform.position = playerTrans.position;
            activeDizzyStars = dizzyGo.AddComponent<OrbitingDizzyStars>();
            activeDizzyStars.AttachToCharacter(playerTrans);

            StartCoroutine(CartoonWobbleRoutine(playerTrans));
        }

        private IEnumerator CartoonWobbleRoutine(Transform character)
        {
            Vector3 basePos = character.position;
            float elapsed = 0f;
            while (gameManager != null && gameManager.CurrentState == GameState.GameOver)
            {
                elapsed += Time.deltaTime * 6f;
                float wobbleAngle = Mathf.Sin(elapsed) * 12f;
                float bob = Mathf.Abs(Mathf.Sin(elapsed * 0.5f)) * 0.08f;

                character.rotation = Quaternion.Euler(0f, 85f, wobbleAngle);
                character.position = basePos + new Vector3(0f, bob, 0f);
                yield return null;
            }
        }

        private void ClearDizzyStars()
        {
            if (activeDizzyStars != null)
            {
                activeDizzyStars.StopOrbit();
                activeDizzyStars = null;
            }
        }
    }
}

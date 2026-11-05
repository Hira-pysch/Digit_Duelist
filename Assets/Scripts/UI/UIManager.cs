using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;


namespace DigitDuelist
{
    /// <summary>
    /// Coordinates UI elements across all panels, binds duel stats, question displays,
    /// answer buttons, health bars, notification popups, and manages input module compatibility.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Subsystems")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private DuelController duelController;

        [Header("Gameplay Panel UI")]
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private TextMeshProUGUI modeBadgeText;
        [SerializeField] private TextMeshProUGUI levelBadgeText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI streakText;

        [Header("Answer Buttons (4 Choices)")]
        [SerializeField] private Button[] choiceButtons = new Button[4];
        [SerializeField] private TextMeshProUGUI[] choiceButtonTexts = new TextMeshProUGUI[4];
        [SerializeField] private Image[] choiceButtonBackgrounds = new Image[4];

        [Header("Health & Timer Bars")]
        [SerializeField] private Slider playerHealthSlider;
        [SerializeField] private TextMeshProUGUI playerHealthText;
        [SerializeField] private Slider enemyHealthSlider;
        [SerializeField] private TextMeshProUGUI enemyHealthText;
        [SerializeField] private TextMeshProUGUI enemyNameText;
        [SerializeField] private Slider turnTimerSlider;

        [Header("Post Level / Rewards Panel")]
        [SerializeField] private TextMeshProUGUI postLevelTitleText;
        [SerializeField] private TextMeshProUGUI postLevelStatsText;

        [Header("Game Over Panel")]
        [SerializeField] private TextMeshProUGUI gameOverScoreText;
        [SerializeField] private TextMeshProUGUI gameOverLevelText;

        [Header("Notification / Toast")]
        [SerializeField] private GameObject notificationContainer;
        [SerializeField] private TextMeshProUGUI notificationText;

        private Color defaultButtonColor = new Color(0.18f, 0.25f, 0.38f, 1f);
        private Color correctButtonColor = new Color(0.15f, 0.65f, 0.25f, 1f);
        private Color wrongButtonColor = new Color(0.75f, 0.2f, 0.2f, 1f);
        private Coroutine questionPopCoroutine;

        private void Awake()
        {
            if (gameManager == null) gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
            if (duelController == null) duelController = FindFirstObjectByType<DuelController>();

            EnsureEventSystemModule();
            AutoFindMissingReferences();
            EnsureHUDExitButton();
            SetupAllButtonListeners();
        }

        private void OnEnable()
        {
            if (duelController != null)
            {
                duelController.OnNewQuestionGenerated += UpdateQuestionUI;
                duelController.OnAnswerEvaluated += HandleAnswerFeedback;
                duelController.OnPlayerHealthChanged += UpdatePlayerHealthUI;
                duelController.OnEnemyHealthChanged += UpdateEnemyHealthUI;
                duelController.OnTimerTick += UpdateTimerUI;
                duelController.OnStreakChanged += UpdateStreakUI;
            }

            if (gameManager != null)
            {
                gameManager.OnStateChanged += HandleStateChanged;
                gameManager.OnNotificationMessage += ShowNotification;
            }
        }

        private void OnDisable()
        {
            if (duelController != null)
            {
                duelController.OnNewQuestionGenerated -= UpdateQuestionUI;
                duelController.OnAnswerEvaluated -= HandleAnswerFeedback;
                duelController.OnPlayerHealthChanged -= UpdatePlayerHealthUI;
                duelController.OnEnemyHealthChanged -= UpdateEnemyHealthUI;
                duelController.OnTimerTick -= UpdateTimerUI;
                duelController.OnStreakChanged -= UpdateStreakUI;
            }

            if (gameManager != null)
            {
                gameManager.OnStateChanged -= HandleStateChanged;
                gameManager.OnNotificationMessage -= ShowNotification;
            }
        }

        private void Start()
        {
            if (notificationContainer != null) notificationContainer.SetActive(false);
        }

        /// <summary>
        /// Ensures EventSystem has the appropriate Input Module (supporting Unity's New Input System and legacy).
        /// In Unity 6 with com.unity.inputsystem, InputSystemUIInputModule must have AssignDefaultActions() called
        /// so mouse clicks, pointer coordinates, and keyboard navigation are bound to UI.
        /// </summary>
        public static void EnsureEventSystemModule()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                eventSystem = esGo.AddComponent<EventSystem>();
            }

            eventSystem.enabled = true;
            GameObject go = eventSystem.gameObject;

            // Remove legacy StandaloneInputModule if conflicting
            StandaloneInputModule standalone = go.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                if (Application.isPlaying)
                    Destroy(standalone);
                else
                    DestroyImmediate(standalone);
            }

            // Ensure GraphicRaycaster on all canvases
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.GetComponent<GraphicRaycaster>() == null)
                {
                    c.gameObject.AddComponent<GraphicRaycaster>();
                }
            }

#if ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.UI.InputSystemUIInputModule inputModule = go.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (inputModule == null)
            {
                inputModule = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            inputModule.AssignDefaultActions();
            Debug.Log("[UIManager] Successfully configured InputSystemUIInputModule with default mouse/touch actions.");
#else
            Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem")
                ?? Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem.ForUI")
                ?? Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, UnityEngine.UI");

            if (inputSystemModuleType != null)
            {
                Component module = go.GetComponent(inputSystemModuleType) ?? go.AddComponent(inputSystemModuleType);
                System.Reflection.MethodInfo assignMethod = inputSystemModuleType.GetMethod("AssignDefaultActions", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                assignMethod?.Invoke(module, null);
                Debug.Log("[UIManager] Assigned default actions to InputSystemUIInputModule via reflection.");
            }
            else
            {
                if (go.GetComponent<StandaloneInputModule>() == null && go.GetComponent<BaseInputModule>() == null)
                {
                    go.AddComponent<StandaloneInputModule>();
                }
            }
#endif
        }

        private void AutoFindMissingReferences()
        {
            // Find choice buttons if unassigned
            for (int i = 0; i < 4; i++)
            {
                if (choiceButtons[i] == null)
                {
                    GameObject btnGo = GameObject.Find($"Btn_Choice_{i}");
                    if (btnGo != null)
                    {
                        choiceButtons[i] = btnGo.GetComponent<Button>();
                        choiceButtonTexts[i] = btnGo.GetComponentInChildren<TextMeshProUGUI>();
                        choiceButtonBackgrounds[i] = btnGo.GetComponent<Image>();
                    }
                }

                if (choiceButtonTexts[i] != null)
                {
                    choiceButtonTexts[i].raycastTarget = false; // Prevent label from intercepting button clicks
                }
            }
        }

        private void EnsureHUDExitButton()
        {
            GameObject exitBtnGo = GameObject.Find("Btn_HUD_Exit");
            if (exitBtnGo == null)
            {
                GameObject pauseBtnGo = GameObject.Find("Btn_HUD_Pause");
                Transform parent = pauseBtnGo != null ? pauseBtnGo.transform.parent : null;
                if (parent == null)
                {
                    GameObject gameplayPanel = GameObject.Find("Gameplay_Panel");
                    if (gameplayPanel != null) parent = gameplayPanel.transform;
                }

                if (parent != null)
                {
                    exitBtnGo = new GameObject("Btn_HUD_Exit");
                    exitBtnGo.transform.SetParent(parent, false);

                    RectTransform rt = exitBtnGo.AddComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(620, 480);
                    rt.sizeDelta = new Vector2(160, 50);

                    Image img = exitBtnGo.AddComponent<Image>();
                    img.color = new Color(0.72f, 0.22f, 0.22f);

                    Button btn = exitBtnGo.AddComponent<Button>();
                    btn.onClick.AddListener(() =>
                    {
                        InGameExitDialog dialog = InGameExitDialog.EnsureInstance();
                        dialog.ShowDialog();
                    });

                    GameObject lbl = new GameObject("Label");
                    lbl.transform.SetParent(exitBtnGo.transform, false);
                    RectTransform lblRt = lbl.AddComponent<RectTransform>();
                    lblRt.anchorMin = Vector2.zero;
                    lblRt.anchorMax = Vector2.one;
                    lblRt.sizeDelta = Vector2.zero;

                    TextMeshProUGUI tmp = lbl.AddComponent<TextMeshProUGUI>();
                    tmp.text = "🚪 EXIT";
                    tmp.fontSize = 22;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = Color.white;
                    tmp.raycastTarget = false;
                }
            }
        }

        /// <summary>
        /// Binds all buttons in the scene directly to GameManager / DuelController methods at runtime.
        /// </summary>
        private void SetupAllButtonListeners()
        {
            // Choice buttons
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int buttonIndex = i;
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].onClick.RemoveAllListeners();
                    choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(buttonIndex));
                }
            }

            // Bind menu buttons by name if present
            BindButton("Btn_Start", () => gameManager?.OnStartButtonPressed());
            BindButton("Btn_Load", () => gameManager?.OnLoadButtonPressed());
            BindButton("Btn_Settings", () => gameManager?.OnSettingsButtonPressed());
            BindButton("Btn_Quit", () => gameManager?.OnQuitButtonPressed());

            BindButton("Btn_Mode_Add", () => gameManager?.SelectAdditionMode());
            BindButton("Btn_Mode_Sub", () => gameManager?.SelectSubtractionMode());
            BindButton("Btn_Mode_Mult", () => gameManager?.SelectMultiplicationMode());
            BindButton("Btn_Mode_Div", () => gameManager?.SelectDivisionMode());
            BindButton("Btn_Mode_MDAS", () => gameManager?.SelectMDASMode());
            BindButton("Btn_Mode_Back", () => gameManager?.OnBackToMainMenuFromModeSelect());

            BindButton("Btn_HUD_Pause", () => gameManager?.PauseGame());
            BindButton("Btn_Resume", () => gameManager?.ResumeGame());
            BindButton("Btn_Save", () => gameManager?.OnPauseMenuSavePressed());
            BindButton("Btn_PauseSettings", () => gameManager?.OnSettingsButtonPressed());
            BindButton("Btn_QuitToMenu", () => gameManager?.OnPauseMenuQuitToMainMenuPressed());

            BindButton("Btn_SettingsBack", () => gameManager?.CloseSettings());

            BindButton("Btn_NextLevel", () => gameManager?.OnProceedNextLevelPressed());
            BindButton("Btn_PostMenu", () => gameManager?.OnPostLevelReturnToMenuPressed());

            BindButton("Btn_Retry", () => gameManager?.OnRetryLevelPressed());
            BindButton("Btn_GameOverMenu", () => gameManager?.OnGameOverReturnToMenuPressed());

            // Kid-Friendly Spells & Mode Toggles
            BindButton("Btn_ZenToggle", () => GameModeManager.Instance?.ToggleZenMode());
            BindButton("Btn_Spell_Freeze", () => AssistSpellsController.Instance?.CastFreezeTime());
            BindButton("Btn_Spell_5050", () => AssistSpellsController.Instance?.CastCrystalBall());
            BindButton("Btn_Spell_Shield", () => AssistSpellsController.Instance?.CastBubbleShield());

            BindButton("Btn_Preset_Novice", () => GameModeManager.Instance?.SetDifficultyPreset(AgeDifficultyPreset.Novice));
            BindButton("Btn_Preset_Adventurer", () => GameModeManager.Instance?.SetDifficultyPreset(AgeDifficultyPreset.Adventurer));
            BindButton("Btn_Preset_Master", () => GameModeManager.Instance?.SetDifficultyPreset(AgeDifficultyPreset.Master));

            // In-Game Exit with Save/Quit options
            BindButton("Btn_HUD_Exit", () =>
            {
                InGameExitDialog dialog = InGameExitDialog.EnsureInstance();
                dialog.ShowDialog();
            });
        }

        private void BindButton(string buttonName, UnityEngine.Events.UnityAction action)
        {
            GameObject go = GameObject.Find(buttonName);
            if (go != null)
            {
                Button btn = go.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(action);

                    // Ensure label doesn't block raycast
                    TextMeshProUGUI label = go.GetComponentInChildren<TextMeshProUGUI>();
                    if (label != null) label.raycastTarget = false;
                }
            }
        }

        private void OnChoiceSelected(int index)
        {
            if (duelController == null || duelController.CurrentQuestion.choices == null) return;
            if (index < 0 || index >= duelController.CurrentQuestion.choices.Length) return;

            int chosenValue = duelController.CurrentQuestion.choices[index];
            PlayerData data = gameManager != null ? gameManager.CurrentPlayerData : null;
            duelController.SubmitAnswer(chosenValue, data);
        }

        private void UpdateQuestionUI(MathQuestion question)
        {
            if (questionText != null)
            {
                questionText.text = question.questionText;
                TriggerQuestionPop();
            }

            // Reset button states and update choice texts
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].interactable = true;
                    choiceButtons[i].transform.localScale = Vector3.one;
                }

                if (choiceButtonBackgrounds != null && i < choiceButtonBackgrounds.Length && choiceButtonBackgrounds[i] != null)
                {
                    choiceButtonBackgrounds[i].color = defaultButtonColor;
                }

                if (choiceButtonTexts != null && i < choiceButtonTexts.Length && choiceButtonTexts[i] != null)
                {
                    if (question.choices != null && i < question.choices.Length)
                    {
                        choiceButtonTexts[i].text = question.choices[i].ToString();
                        choiceButtonTexts[i].raycastTarget = false;
                    }
                }
            }

            PlayerData data = gameManager != null ? gameManager.CurrentPlayerData : null;
            if (data != null)
            {
                if (levelBadgeText != null) levelBadgeText.text = $"Level {data.currentLevel}";
                if (modeBadgeText != null) modeBadgeText.text = $"Mode: {data.chosenMode}";
                if (scoreText != null) scoreText.text = $"Score: {data.score:N0}";
            }

            if (enemyNameText != null && duelController != null)
            {
                enemyNameText.text = duelController.CurrentEnemyName;
            }
        }

        private void TriggerQuestionPop()
        {
            if (questionText == null) return;
            if (questionPopCoroutine != null) StopCoroutine(questionPopCoroutine);
            questionPopCoroutine = StartCoroutine(QuestionPopRoutine());
        }

        private IEnumerator QuestionPopRoutine()
        {
            Transform t = questionText.transform;
            Vector3 baseScale = Vector3.one;
            float duration = 0.26f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float s = 1f + Mathf.Sin(progress * Mathf.PI) * 0.18f;
                t.localScale = baseScale * s;
                yield return null;
            }

            t.localScale = baseScale;
            questionPopCoroutine = null;
        }

        private void HandleAnswerFeedback(bool isCorrect, int correctAnswer)
        {
            // Highlight buttons with spring bounce and shake animations
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].interactable = false;
                }

                if (duelController != null && duelController.CurrentQuestion.choices != null && i < duelController.CurrentQuestion.choices.Length)
                {
                    int val = duelController.CurrentQuestion.choices[i];
                    RectTransform rt = choiceButtons[i] != null ? choiceButtons[i].GetComponent<RectTransform>() : null;

                    if (val == correctAnswer)
                    {
                        if (choiceButtonBackgrounds != null && i < choiceButtonBackgrounds.Length && choiceButtonBackgrounds[i] != null)
                        {
                            choiceButtonBackgrounds[i].color = correctButtonColor;
                        }
                        if (rt != null) StartCoroutine(ButtonBounceRoutine(rt));
                    }
                    else if (!isCorrect && choiceButtonBackgrounds != null && i < choiceButtonBackgrounds.Length && choiceButtonBackgrounds[i] != null)
                    {
                        choiceButtonBackgrounds[i].color = wrongButtonColor;
                        if (rt != null) StartCoroutine(ButtonShakeRoutine(rt));
                    }
                }
            }
        }

        private IEnumerator ButtonBounceRoutine(RectTransform rt)
        {
            if (rt == null) yield break;
            Vector3 baseScale = Vector3.one;
            float duration = 0.28f;
            float elapsed = 0f;

            while (elapsed < duration && rt != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float s = 1f + Mathf.Sin(t * Mathf.PI) * 0.22f;
                rt.localScale = baseScale * s;
                yield return null;
            }

            if (rt != null) rt.localScale = baseScale;
        }

        private IEnumerator ButtonShakeRoutine(RectTransform rt)
        {
            if (rt == null) yield break;
            Vector2 basePos = rt.anchoredPosition;
            float duration = 0.32f;
            float elapsed = 0f;

            while (elapsed < duration && rt != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float offset = Mathf.Sin(elapsed * 42f) * (1f - t) * 14f;
                rt.anchoredPosition = new Vector2(basePos.x + offset, basePos.y);
                yield return null;
            }

            if (rt != null) rt.anchoredPosition = basePos;
        }

        private void UpdatePlayerHealthUI(int current, int max)
        {
            if (playerHealthSlider != null)
            {
                playerHealthSlider.maxValue = max;
                playerHealthSlider.value = current;
            }

            if (playerHealthText != null)
            {
                playerHealthText.text = $"{current} / {max}";
            }
        }

        private void UpdateEnemyHealthUI(int current, int max)
        {
            if (enemyHealthSlider != null)
            {
                enemyHealthSlider.maxValue = max;
                enemyHealthSlider.value = current;
            }

            if (enemyHealthText != null)
            {
                enemyHealthText.text = $"{current} / {max}";
            }
        }

        private void UpdateTimerUI(float remaining, float max)
        {
            if (turnTimerSlider != null)
            {
                turnTimerSlider.maxValue = max;
                turnTimerSlider.value = remaining;
            }
        }

        private void UpdateStreakUI(int streak)
        {
            if (streakText != null)
            {
                streakText.text = streak > 1 ? $"🔥 Streak: x{streak}" : "";
            }
        }

        private void HandleStateChanged(GameState state)
        {
            PlayerData data = gameManager != null ? gameManager.CurrentPlayerData : null;

            if (state == GameState.LevelComplete)
            {
                if (postLevelTitleText != null)
                {
                    postLevelTitleText.text = $"Level {((data != null) ? data.currentLevel - 1 : 1)} Cleared!";
                }

                if (postLevelStatsText != null && data != null)
                {
                    postLevelStatsText.text = $"Total Score: {data.score:N0}\nAttack Power: {data.attackPower}\nMax Health: {data.maxHealth}\nXP to next level: {data.xp}/{data.xpToNextLevel}";
                }
            }
            else if (state == GameState.GameOver)
            {
                if (gameOverScoreText != null && data != null)
                {
                    gameOverScoreText.text = $"Final Score: {data.score:N0}";
                }

                if (gameOverLevelText != null && data != null)
                {
                    gameOverLevelText.text = $"Reached Level {data.currentLevel} (Streak: {data.highestStreak})";
                }
            }
        }

        public void ShowNotification(string message)
        {
            if (notificationContainer != null && notificationText != null)
            {
                notificationText.text = message;
                StopCoroutine(nameof(HideNotificationRoutine));
                StartCoroutine(HideNotificationRoutine());
            }
        }

        private IEnumerator HideNotificationRoutine()
        {
            notificationContainer.SetActive(true);
            yield return new WaitForSecondsRealtime(2.5f);
            notificationContainer.SetActive(false);
        }
    }
}

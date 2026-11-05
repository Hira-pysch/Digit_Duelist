using System;
using UnityEngine;

namespace DigitDuelist
{
    /// <summary>
    /// Coordinates kid-friendly game modes:
    /// 1. Zen / Untimed Mode toggle (disables pressure for young learners).
    /// 2. Age-Based Difficulty Presets (Novice 5-7, Adventurer 8-9, Master 10+).
    /// 3. Keyboard accessibility (1, 2, 3, 4 numeric hotkeys and spell shortcuts).
    /// </summary>
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        [Header("Mode & Preset Settings")]
        [SerializeField] private bool isZenMode = false;
        [SerializeField] private AgeDifficultyPreset activePreset = AgeDifficultyPreset.Adventurer;

        [Header("Keyboard Controls Enabled")]
        [SerializeField] private bool enableKeyboardShortcuts = true;

        [Header("Subsystems")]
        [SerializeField] private DuelController duelController;
        [SerializeField] private MathGenerator mathGenerator;

        // Events
        public event Action<bool> OnZenModeToggled;
        public event Action<AgeDifficultyPreset> OnDifficultyPresetChanged;

        public bool IsZenMode => isZenMode;
        public AgeDifficultyPreset ActivePreset => activePreset;

        private const string PrefKey_ZenMode = "DigitDuelist_ZenMode";
        private const string PrefKey_Preset = "DigitDuelist_DifficultyPreset";

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            LoadPreferences();
            FindSubsystems();
        }

        private void Start()
        {
            ApplyZenMode();
            ApplyDifficultyPreset();
        }

        private void Update()
        {
            if (!enableKeyboardShortcuts) return;

            // Only process during active gameplay
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Gameplay) return;

            HandleKeyboardAnswerInput();
            HandleKeyboardSpellInput();
        }

        private void FindSubsystems()
        {
            if (duelController == null) duelController = FindFirstObjectByType<DuelController>();
            if (mathGenerator == null) mathGenerator = FindFirstObjectByType<MathGenerator>();
        }

        private void LoadPreferences()
        {
            isZenMode = PlayerPrefs.GetInt(PrefKey_ZenMode, 0) == 1;
            activePreset = (AgeDifficultyPreset)PlayerPrefs.GetInt(PrefKey_Preset, (int)AgeDifficultyPreset.Adventurer);
        }

        #region Zen / Untimed Mode

        public void SetZenMode(bool enabled)
        {
            isZenMode = enabled;
            PlayerPrefs.SetInt(PrefKey_ZenMode, isZenMode ? 1 : 0);
            PlayerPrefs.Save();

            ApplyZenMode();
            OnZenModeToggled?.Invoke(isZenMode);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.Notify(isZenMode ? "🧘 Zen Mode ON: Untimed relaxed learning!" : "⏱️ Timed Mode ON: Standard duel timer active!");
            }
        }

        public void ToggleZenMode() => SetZenMode(!isZenMode);

        private void ApplyZenMode()
        {
            if (duelController == null) FindSubsystems();

            // Hide/Show timer bar
            GameObject timerBar = GameObject.Find("TimerBar");
            if (timerBar != null)
            {
                timerBar.SetActive(!isZenMode);
            }
        }

        #endregion

        #region Age Difficulty Presets

        public void SetDifficultyPreset(AgeDifficultyPreset preset)
        {
            activePreset = preset;
            PlayerPrefs.SetInt(PrefKey_Preset, (int)activePreset);
            PlayerPrefs.Save();

            ApplyDifficultyPreset();
            OnDifficultyPresetChanged?.Invoke(activePreset);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.Notify($"Preset Changed: {activePreset.GetDisplayName()}");
            }
        }

        private void ApplyDifficultyPreset()
        {
            if (mathGenerator == null) FindSubsystems();
            // MathGenerator questions will read activePreset
        }

        #endregion

        #region Keyboard Accessibility

        private void HandleKeyboardAnswerInput()
        {
            if (duelController == null || duelController.CurrentQuestion.choices == null) return;

            int choiceIndex = -1;

            if (GameInput.GetKeyDown(KeyCode.Alpha1) || GameInput.GetKeyDown(KeyCode.Keypad1)) choiceIndex = 0;
            else if (GameInput.GetKeyDown(KeyCode.Alpha2) || GameInput.GetKeyDown(KeyCode.Keypad2)) choiceIndex = 1;
            else if (GameInput.GetKeyDown(KeyCode.Alpha3) || GameInput.GetKeyDown(KeyCode.Keypad3)) choiceIndex = 2;
            else if (GameInput.GetKeyDown(KeyCode.Alpha4) || GameInput.GetKeyDown(KeyCode.Keypad4)) choiceIndex = 3;

            if (choiceIndex >= 0 && choiceIndex < duelController.CurrentQuestion.choices.Length)
            {
                // Trigger answer
                int chosenVal = duelController.CurrentQuestion.choices[choiceIndex];
                PlayerData data = GameManager.Instance != null ? GameManager.Instance.CurrentPlayerData : null;
                duelController.SubmitAnswer(chosenVal, data);
            }
        }

        private void HandleKeyboardSpellInput()
        {
            if (AssistSpellsController.Instance == null) return;

            // Q: Freeze, W: 50/50, E: Shield
            if (GameInput.GetKeyDown(KeyCode.Q))
            {
                AssistSpellsController.Instance.CastFreezeTime();
            }
            else if (GameInput.GetKeyDown(KeyCode.W))
            {
                AssistSpellsController.Instance.CastCrystalBall();
            }
            else if (GameInput.GetKeyDown(KeyCode.E))
            {
                AssistSpellsController.Instance.CastBubbleShield();
            }
        }

        #endregion
    }
}

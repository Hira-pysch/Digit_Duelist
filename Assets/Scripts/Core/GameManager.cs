using System;
using UnityEngine;

namespace DigitDuelist
{
    /// <summary>
    /// The global State Machine and central coordinator for Digit Duelist.
    /// Controls single-scene panel activation, transitions, game loop, and save pipeline.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Current State")]
        [SerializeField] private GameState currentState = GameState.MainMenu;
        private GameState previousState = GameState.MainMenu;

        [Header("UI Panels")]
        [Tooltip("Corresponds to MainMenu_Panel (Start, Load, Settings, Quit)")]
        [SerializeField] private GameObject mainMenuPanel;

        [Tooltip("Corresponds to GameMode_Panel (Addition, Subtraction, Mult, Div, MDAS)")]
        [SerializeField] private GameObject gameModePanel;

        [Tooltip("Corresponds to Gameplay_Panel (Math problem, timers, health bars)")]
        [SerializeField] private GameObject gameplayPanel;

        [Tooltip("Corresponds to PauseMenu_Panel (Resume, Save, Settings, Quit)")]
        [SerializeField] private GameObject pauseMenuPanel;

        [Tooltip("Corresponds to PostLevel_Panel (Level Complete, Rewards, Next Level)")]
        [SerializeField] private GameObject postLevelPanel;

        [Tooltip("Corresponds to Settings_Panel (Audio, Display, Back)")]
        [SerializeField] private GameObject settingsPanel;

        [Tooltip("Corresponds to GameOver_Panel (Retry, Main Menu)")]
        [SerializeField] private GameObject gameOverPanel;

        [Tooltip("Optional LoadGame panel / confirmation dialog")]
        [SerializeField] private GameObject loadGamePanel;

        [Header("Subsystems")]
        [SerializeField] private DuelController duelController;
        [SerializeField] private MathGenerator mathGenerator;

        [Header("Runtime Player Data")]
        [SerializeField] private PlayerData currentPlayerData;

        // Events
        public event Action<GameState> OnStateChanged;
        public event Action<string> OnNotificationMessage;

        public GameState CurrentState => currentState;
        public GameState PreviousState => previousState;
        public PlayerData CurrentPlayerData => currentPlayerData;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (currentPlayerData == null)
            {
                currentPlayerData = new PlayerData();
            }

            FindSubsystems();
        }

        private void Start()
        {
            // Initial state entry
            ChangeState(GameState.MainMenu);
        }

        private void Update()
        {
            // Allow toggling Pause via Escape key during Gameplay or Paused
            if (GameInput.GetKeyDown(KeyCode.Escape))
            {
                if (currentState == GameState.Gameplay)
                {
                    InGameExitDialog dialog = InGameExitDialog.EnsureInstance();
                    dialog.ShowDialog();
                }
                else if (currentState == GameState.Paused)
                {
                    ResumeGame();
                }
                else if (currentState == GameState.Settings)
                {
                    CloseSettings();
                }
            }

            // Debug Shortcut: Press 'L' during Gameplay to jump directly to Level 5 Boss!
            if (GameInput.GetKeyDown(KeyCode.L) && currentState == GameState.Gameplay)
            {
                if (currentPlayerData != null)
                {
                    currentPlayerData.currentLevel = 5;
                    Notify("👑 Level set to 5! Spawning Boss Duel...");
                    if (duelController != null) duelController.StartDuel(currentPlayerData);
                }
            }
        }

        private void FindSubsystems()
        {
            if (duelController == null) duelController = GetComponent<DuelController>() ?? FindFirstObjectByType<DuelController>() ?? gameObject.AddComponent<DuelController>();
            if (mathGenerator == null) mathGenerator = GetComponent<MathGenerator>() ?? FindFirstObjectByType<MathGenerator>() ?? gameObject.AddComponent<MathGenerator>();

            // Auto-ensure all modular managers exist at runtime
            if (FindFirstObjectByType<LevelUpgradeManager>() == null) gameObject.AddComponent<LevelUpgradeManager>();
            if (FindFirstObjectByType<BossChoiceFlipper>() == null) gameObject.AddComponent<BossChoiceFlipper>();
            if (FindFirstObjectByType<StageBackgroundManager>() == null) gameObject.AddComponent<StageBackgroundManager>();
            if (FindFirstObjectByType<KidAudioManager>() == null) gameObject.AddComponent<KidAudioManager>();
            if (FindFirstObjectByType<GameModeManager>() == null) gameObject.AddComponent<GameModeManager>();
            if (FindFirstObjectByType<AssistSpellsController>() == null) gameObject.AddComponent<AssistSpellsController>();
            if (FindFirstObjectByType<KidFeedbackManager>() == null) gameObject.AddComponent<KidFeedbackManager>();
            if (FindFirstObjectByType<SkinCustomizationManager>() == null) gameObject.AddComponent<SkinCustomizationManager>();
        }

        /// <summary>
        /// Transitions the state machine to a new state and manages panel visibility.
        /// </summary>
        public void ChangeState(GameState newState)
        {
            if (currentState != newState && currentState != GameState.Settings)
            {
                previousState = currentState;
            }

            currentState = newState;
            Debug.Log($"[GameManager] State changed to: {currentState}");

            UpdateUIPanels();
            HandleStateEntry(newState);

            OnStateChanged?.Invoke(currentState);
        }

        /// <summary>
        /// Updates SetActive(true/false) on UI canvas panels matching flowchart node mapping.
        /// </summary>
        private void UpdateUIPanels()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(currentState == GameState.MainMenu);
            if (gameModePanel != null) gameModePanel.SetActive(currentState == GameState.ModeSelection);
            if (gameplayPanel != null) gameplayPanel.SetActive(currentState == GameState.Gameplay);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(currentState == GameState.Paused);
            if (postLevelPanel != null) postLevelPanel.SetActive(currentState == GameState.LevelComplete);
            if (settingsPanel != null) settingsPanel.SetActive(currentState == GameState.Settings);
            if (gameOverPanel != null) gameOverPanel.SetActive(currentState == GameState.GameOver);
            if (loadGamePanel != null) loadGamePanel.SetActive(currentState == GameState.LoadGame);
        }

        private void HandleStateEntry(GameState state)
        {
            switch (state)
            {
                case GameState.MainMenu:
                    Time.timeScale = 1f;
                    break;

                case GameState.ModeSelection:
                    Time.timeScale = 1f;
                    break;

                case GameState.LoadGame:
                    ExecuteLoadGame();
                    break;

                case GameState.Gameplay:
                    Time.timeScale = 1f;
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f; // Freeze game simulation on pause
                    break;

                case GameState.LevelComplete:
                    Time.timeScale = 1f;
                    // Auto-save on level victory as specified in progression pipeline
                    SaveCurrentGame();
                    break;

                case GameState.GameOver:
                    Time.timeScale = 1f;
                    break;

                case GameState.Settings:
                    // Keep existing timeScale
                    break;
            }
        }

        #region Flowchart Transition Buttons

        // --- Main Menu Buttons ---

        public void OnStartButtonPressed()
        {
            ChangeState(GameState.ModeSelection);
        }

        public void OnLoadButtonPressed()
        {
            ChangeState(GameState.LoadGame);
        }

        public void OnSettingsButtonPressed()
        {
            previousState = currentState;
            ChangeState(GameState.Settings);
        }

        public void OnQuitButtonPressed()
        {
            Debug.Log("[GameManager] Quitting Application...");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // --- Mode Selection Buttons ---

        public void SelectAdditionMode() => SelectMode(MathOperation.Addition);
        public void SelectSubtractionMode() => SelectMode(MathOperation.Subtraction);
        public void SelectMultiplicationMode() => SelectMode(MathOperation.Multiplication);
        public void SelectDivisionMode() => SelectMode(MathOperation.Division);
        public void SelectMDASMode() => SelectMode(MathOperation.MDAS);

        public void SelectMode(MathOperation mode)
        {
            if (currentPlayerData == null)
            {
                currentPlayerData = new PlayerData();
            }
            else
            {
                currentPlayerData.ResetToDefaults();
            }

            currentPlayerData.chosenMode = mode;

            if (mathGenerator != null)
            {
                mathGenerator.SelectedMode = mode;
            }

            ChangeState(GameState.Gameplay);

            if (duelController != null)
            {
                duelController.StartDuel(currentPlayerData);
            }
        }

        public void OnBackToMainMenuFromModeSelect()
        {
            ChangeState(GameState.MainMenu);
        }

        // --- Pause Menu Buttons ---

        public void PauseGame()
        {
            ChangeState(GameState.Paused);
        }

        public void ResumeGame()
        {
            ChangeState(GameState.Gameplay);
        }

        public void SaveCurrentGame()
        {
            if (currentPlayerData != null)
            {
                bool saved = SaveManager.SaveGame(currentPlayerData);
                if (saved)
                {
                    Notify("Game Saved Successfully!");
                }
                else
                {
                    Notify("Failed to Save Game.");
                }
            }
        }

        public void OnPauseMenuSavePressed()
        {
            SaveCurrentGame();
        }

        public void OnPauseMenuQuitToMainMenuPressed()
        {
            SaveCurrentGame();
            ChangeState(GameState.MainMenu);
        }

        // --- Settings Menu ---

        public void CloseSettings()
        {
            // Return to where we came from (MainMenu or Paused)
            ChangeState(previousState);
        }

        // --- Load Game Logic ---

        private void ExecuteLoadGame()
        {
            if (SaveManager.HasSaveData())
            {
                PlayerData loaded = SaveManager.LoadGame();
                if (loaded != null)
                {
                    currentPlayerData = loaded;
                    if (mathGenerator != null)
                    {
                        mathGenerator.SelectedMode = currentPlayerData.chosenMode;
                    }

                    Notify($"Loaded Level {currentPlayerData.currentLevel} Save!");
                    ChangeState(GameState.Gameplay);

                    if (duelController != null)
                    {
                        duelController.StartDuel(currentPlayerData);
                    }
                    return;
                }
            }

            Notify("No save file found! Starting new game.");
            ChangeState(GameState.ModeSelection);
        }

        // --- Combat Triggers (from DuelController) ---

        public void OnLevelComplete()
        {
            if (currentPlayerData != null)
            {
                currentPlayerData.LevelUp();
            }
            ChangeState(GameState.LevelComplete);
        }

        public void OnPlayerDefeated()
        {
            ChangeState(GameState.GameOver);
        }

        public void OnGameOver()
        {
            OnPlayerDefeated();
        }

        // --- Post-Level / Level Complete Buttons ---

        public void OnProceedNextLevelPressed()
        {
            // Save progression data before proceeding to next level
            SaveCurrentGame();

            // 5-Level Milestone: If player completed level 5, 10, 15, pause transition for upgrade selection
            int completedLevel = (currentPlayerData != null) ? currentPlayerData.currentLevel - 1 : 1;
            if (LevelUpgradeManager.Instance != null && LevelUpgradeManager.Instance.IsMilestoneLevel(completedLevel))
            {
                LevelUpgradeManager.Instance.PromptUpgradeSelection(() =>
                {
                    ProceedToNextDuel();
                });
                return;
            }

            ProceedToNextDuel();
        }

        private void ProceedToNextDuel()
        {
            ChangeState(GameState.Gameplay);

            if (duelController != null)
            {
                duelController.StartDuel(currentPlayerData);
            }
        }

        public void OnPostLevelReturnToMenuPressed()
        {
            SaveCurrentGame();
            ChangeState(GameState.MainMenu);
        }

        // --- Game Over Buttons ---

        public void OnRetryLevelPressed()
        {
            if (currentPlayerData != null)
            {
                currentPlayerData.ResetHealth();
            }

            ChangeState(GameState.Gameplay);

            if (duelController != null)
            {
                duelController.StartDuel(currentPlayerData);
            }
        }

        public void OnGameOverReturnToMenuPressed()
        {
            ChangeState(GameState.MainMenu);
        }

        #endregion

        public void Notify(string message)
        {
            Debug.Log($"[DigitDuelist Notification] {message}");
            OnNotificationMessage?.Invoke(message);
        }
    }
}

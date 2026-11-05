#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Reflection;

namespace DigitDuelist.Editor
{
    public class DigitDuelistSetupEditor : EditorWindow
    {
        [MenuItem("Tools/Digit Duelist/Create Complete UI Canvas & Managers", false, 1)]
        public static void CreateCompleteSetup()
        {
            // 1. Ensure EventSystem exists and has the correct Input Module for Unity 6 / New Input System
            EnsureEventSystem();

            // 2. Create Managers GameObject
            GameObject managersGo = GameObject.Find("[GameManagers]");
            if (managersGo == null)
            {
                managersGo = new GameObject("[GameManagers]");
                Undo.RegisterCreatedObjectUndo(managersGo, "Create GameManagers");
            }

            GameManager gameManager = managersGo.GetComponent<GameManager>() ?? Undo.AddComponent<GameManager>(managersGo);
            DuelController duelController = managersGo.GetComponent<DuelController>() ?? Undo.AddComponent<DuelController>(managersGo);
            MathGenerator mathGenerator = managersGo.GetComponent<MathGenerator>() ?? Undo.AddComponent<MathGenerator>(managersGo);
            UIManager uiManager = managersGo.GetComponent<UIManager>() ?? Undo.AddComponent<UIManager>(managersGo);

            // 3. Create Canvas
            GameObject canvasGo = GameObject.Find("DigitDuelist_Canvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("DigitDuelist_Canvas");
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create DigitDuelist_Canvas");

                Canvas canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();
            }

            Transform canvasRoot = canvasGo.transform;

            // 4. Create Panels
            GameObject mainMenuPanel = CreatePanel("MainMenu_Panel", canvasRoot, new Color(0.08f, 0.1f, 0.15f, 0.95f), true);
            GameObject gameModePanel = CreatePanel("GameMode_Panel", canvasRoot, new Color(0.08f, 0.12f, 0.18f, 0.95f), true);
            GameObject gameplayPanel = CreatePanel("Gameplay_Panel", canvasRoot, new Color(0f, 0f, 0f, 0f), false); // Transparent - MUST NOT BLOCK RAYCASTS
            GameObject pauseMenuPanel = CreatePanel("PauseMenu_Panel", canvasRoot, new Color(0.05f, 0.05f, 0.08f, 0.92f), true);
            GameObject postLevelPanel = CreatePanel("PostLevel_Panel", canvasRoot, new Color(0.06f, 0.14f, 0.1f, 0.96f), true);
            GameObject settingsPanel = CreatePanel("Settings_Panel", canvasRoot, new Color(0.08f, 0.1f, 0.14f, 0.95f), true);
            GameObject gameOverPanel = CreatePanel("GameOver_Panel", canvasRoot, new Color(0.18f, 0.06f, 0.06f, 0.96f), true);
            GameObject notificationContainer = CreatePanel("Notification_Panel", canvasRoot, new Color(0.1f, 0.4f, 0.7f, 0.95f), false, new Vector2(0.5f, 0.9f), new Vector2(500, 70));

            // Populate Main Menu Panel
            CreateTitle(mainMenuPanel.transform, "DIGIT DUELIST", new Vector2(0, 220), 72, new Color(1f, 0.85f, 0.3f));
            CreateSubtitle(mainMenuPanel.transform, "Arithmetic Combat & Mathematical Mastery", new Vector2(0, 150), 26);
            CreateButton(mainMenuPanel.transform, "Btn_Start", "START GAME", new Vector2(0, 40), new Vector2(340, 65), ButtonActionType.StartGame);
            CreateButton(mainMenuPanel.transform, "Btn_Load", "LOAD GAME", new Vector2(0, -40), new Vector2(340, 65), ButtonActionType.LoadGame);
            CreateButton(mainMenuPanel.transform, "Btn_Settings", "SETTINGS", new Vector2(0, -120), new Vector2(340, 65), ButtonActionType.OpenSettings);
            CreateButton(mainMenuPanel.transform, "Btn_Quit", "QUIT", new Vector2(0, -200), new Vector2(340, 65), ButtonActionType.QuitApp);

            // Populate Mode Selection Panel
            CreateTitle(gameModePanel.transform, "SELECT GAME MODE", new Vector2(0, 240), 56, new Color(0.4f, 0.85f, 1f));
            CreateSubtitle(gameModePanel.transform, "Choose your mathematical discipline for this run", new Vector2(0, 180), 24);
            CreateButton(gameModePanel.transform, "Btn_Mode_Add", "ADDITION (+)", new Vector2(0, 80), new Vector2(380, 55), ButtonActionType.SelectAddition);
            CreateButton(gameModePanel.transform, "Btn_Mode_Sub", "SUBTRACTION (-)", new Vector2(0, 15), new Vector2(380, 55), ButtonActionType.SelectSubtraction);
            CreateButton(gameModePanel.transform, "Btn_Mode_Mult", "MULTIPLICATION (×)", new Vector2(0, -50), new Vector2(380, 55), ButtonActionType.SelectMultiplication);
            CreateButton(gameModePanel.transform, "Btn_Mode_Div", "DIVISION (÷)", new Vector2(0, -115), new Vector2(380, 55), ButtonActionType.SelectDivision);
            CreateButton(gameModePanel.transform, "Btn_Mode_MDAS", "✨ MDAS (MIXED RANDOM) ✨", new Vector2(0, -180), new Vector2(440, 60), ButtonActionType.SelectMDAS, new Color(0.9f, 0.4f, 0.1f));
            CreateButton(gameModePanel.transform, "Btn_Mode_Back", "← BACK TO MENU", new Vector2(0, -260), new Vector2(280, 48), ButtonActionType.BackToMainMenuFromMode, new Color(0.4f, 0.4f, 0.45f));

            // Populate Pause Menu Panel
            CreateTitle(pauseMenuPanel.transform, "GAME PAUSED", new Vector2(0, 200), 58, Color.white);
            CreateButton(pauseMenuPanel.transform, "Btn_Resume", "RESUME", new Vector2(0, 60), new Vector2(320, 60), ButtonActionType.ResumeGame);
            CreateButton(pauseMenuPanel.transform, "Btn_Save", "SAVE GAME", new Vector2(0, -15), new Vector2(320, 60), ButtonActionType.SaveGame, new Color(0.2f, 0.65f, 0.35f));
            CreateButton(pauseMenuPanel.transform, "Btn_PauseSettings", "SETTINGS", new Vector2(0, -90), new Vector2(320, 60), ButtonActionType.OpenSettings);
            CreateButton(pauseMenuPanel.transform, "Btn_QuitToMenu", "SAVE & QUIT TO MENU", new Vector2(0, -165), new Vector2(360, 60), ButtonActionType.QuitToMainMenuFromPause, new Color(0.7f, 0.25f, 0.25f));

            // Populate Settings Panel
            CreateTitle(settingsPanel.transform, "SETTINGS", new Vector2(0, 200), 56, Color.white);
            CreateSubtitle(settingsPanel.transform, "Game Options & Preferences", new Vector2(0, 140), 24);
            CreateButton(settingsPanel.transform, "Btn_SettingsBack", "← BACK", new Vector2(0, -200), new Vector2(280, 55), ButtonActionType.CloseSettings);

            // Populate Post-Level Panel
            TextMeshProUGUI postLevelTitle = CreateTitle(postLevelPanel.transform, "LEVEL COMPLETE!", new Vector2(0, 200), 62, new Color(0.3f, 1f, 0.45f));
            TextMeshProUGUI postLevelStats = CreateSubtitle(postLevelPanel.transform, "Stats & Rewards", new Vector2(0, 50), 28);
            CreateButton(postLevelPanel.transform, "Btn_NextLevel", "PROCEED NEXT LEVEL →", new Vector2(0, -100), new Vector2(400, 65), ButtonActionType.ProceedNextLevel, new Color(0.15f, 0.7f, 0.3f));
            CreateButton(postLevelPanel.transform, "Btn_PostMenu", "MAIN MENU", new Vector2(0, -180), new Vector2(320, 55), ButtonActionType.PostLevelToMainMenu, new Color(0.4f, 0.4f, 0.45f));

            // Populate Game Over Panel
            CreateTitle(gameOverPanel.transform, "DEFEAT", new Vector2(0, 200), 64, new Color(1f, 0.3f, 0.3f));
            TextMeshProUGUI gameOverScore = CreateSubtitle(gameOverPanel.transform, "Final Score: 0", new Vector2(0, 90), 32);
            TextMeshProUGUI gameOverLevel = CreateSubtitle(gameOverPanel.transform, "Level Reached: 1", new Vector2(0, 40), 26);
            CreateButton(gameOverPanel.transform, "Btn_Retry", "RETRY LEVEL", new Vector2(0, -70), new Vector2(340, 65), ButtonActionType.RetryLevel, new Color(0.85f, 0.5f, 0.1f));
            CreateButton(gameOverPanel.transform, "Btn_GameOverMenu", "MAIN MENU", new Vector2(0, -150), new Vector2(320, 55), ButtonActionType.GameOverToMainMenu, new Color(0.4f, 0.4f, 0.45f));

            // Populate Gameplay Panel HUD
            TextMeshProUGUI levelBadge = CreateBadge(gameplayPanel.transform, "Level 1", new Vector2(-750, 480), 28, TextAlignmentOptions.Left);
            TextMeshProUGUI modeBadge = CreateBadge(gameplayPanel.transform, "Mode: MDAS", new Vector2(-750, 440), 22, TextAlignmentOptions.Left);
            TextMeshProUGUI scoreBadge = CreateBadge(gameplayPanel.transform, "Score: 0", new Vector2(0, 480), 30, TextAlignmentOptions.Center);
            TextMeshProUGUI streakBadge = CreateBadge(gameplayPanel.transform, "", new Vector2(0, 435), 24, TextAlignmentOptions.Center, new Color(1f, 0.75f, 0.2f));

            // Pause button Top-Right
            CreateButton(gameplayPanel.transform, "Btn_HUD_Pause", "⏸ PAUSE", new Vector2(800, 480), new Vector2(160, 50), ButtonActionType.PauseGame, new Color(0.3f, 0.35f, 0.45f));

            // Player & Enemy Health Bars
            Slider playerSlider = CreateHealthBar(gameplayPanel.transform, "PlayerHealthBar", new Vector2(-550, -250), new Color(0.2f, 0.75f, 0.3f));
            TextMeshProUGUI playerHpText = CreateBadge(gameplayPanel.transform, "HP: 100 / 100", new Vector2(-550, -200), 22, TextAlignmentOptions.Center);
            CreateBadge(gameplayPanel.transform, "Player (Knight)", new Vector2(-550, -290), 20, TextAlignmentOptions.Center, new Color(0.8f, 0.9f, 1f));

            Slider enemySlider = CreateHealthBar(gameplayPanel.transform, "EnemyHealthBar", new Vector2(550, -250), new Color(0.85f, 0.2f, 0.2f));
            TextMeshProUGUI enemyHpText = CreateBadge(gameplayPanel.transform, "Enemy: 50 / 50", new Vector2(550, -200), 22, TextAlignmentOptions.Center);
            TextMeshProUGUI enemyName = CreateBadge(gameplayPanel.transform, "Enemy Challenger", new Vector2(550, -290), 20, TextAlignmentOptions.Center, new Color(1f, 0.8f, 0.8f));

            // Turn Timer Bar
            Slider timerSlider = CreateHealthBar(gameplayPanel.transform, "TimerBar", new Vector2(0, 140), new Color(1f, 0.85f, 0.25f), new Vector2(600, 16));

            // Question Box (Center)
            GameObject questionBox = CreatePanel("QuestionCard", gameplayPanel.transform, new Color(0.1f, 0.14f, 0.22f, 0.95f), true, new Vector2(0.5f, 0.5f), new Vector2(680, 140));
            questionBox.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);
            TextMeshProUGUI questionText = CreateTitle(questionBox.transform, "12 + 7 = ?", new Vector2(0, 0), 54, Color.white);

            // 4 Multiple Choice Buttons (Bottom Center)
            Button[] choiceBtns = new Button[4];
            TextMeshProUGUI[] choiceTexts = new TextMeshProUGUI[4];
            Image[] choiceBgs = new Image[4];

            Vector2[] choicePositions = new Vector2[]
            {
                new Vector2(-160, -110),
                new Vector2(160, -110),
                new Vector2(-160, -190),
                new Vector2(160, -190)
            };

            for (int i = 0; i < 4; i++)
            {
                ButtonActionType choiceAction = (ButtonActionType)((int)ButtonActionType.Choice0 + i);
                GameObject btnGo = CreateButton(gameplayPanel.transform, $"Btn_Choice_{i}", $"Choice {i+1}", choicePositions[i], new Vector2(290, 65), choiceAction, new Color(0.18f, 0.25f, 0.38f));
                choiceBtns[i] = btnGo.GetComponent<Button>();
                choiceTexts[i] = btnGo.GetComponentInChildren<TextMeshProUGUI>();
                choiceBgs[i] = btnGo.GetComponent<Image>();
            }

            // Notification UI
            TextMeshProUGUI notifText = CreateTitle(notificationContainer.transform, "Game Saved!", Vector2.zero, 24, Color.white);
            notifText.raycastTarget = false;

            // 5. Wire Inspector references
            Undo.RecordObject(gameManager, "Assign GameManager References");
            SetPrivateField(gameManager, "mainMenuPanel", mainMenuPanel);
            SetPrivateField(gameManager, "gameModePanel", gameModePanel);
            SetPrivateField(gameManager, "gameplayPanel", gameplayPanel);
            SetPrivateField(gameManager, "pauseMenuPanel", pauseMenuPanel);
            SetPrivateField(gameManager, "postLevelPanel", postLevelPanel);
            SetPrivateField(gameManager, "settingsPanel", settingsPanel);
            SetPrivateField(gameManager, "gameOverPanel", gameOverPanel);
            SetPrivateField(gameManager, "duelController", duelController);
            SetPrivateField(gameManager, "mathGenerator", mathGenerator);

            Undo.RecordObject(duelController, "Assign DuelController References");
            SetPrivateField(duelController, "mathGenerator", mathGenerator);

            Undo.RecordObject(uiManager, "Assign UIManager References");
            SetPrivateField(uiManager, "gameManager", gameManager);
            SetPrivateField(uiManager, "duelController", duelController);
            SetPrivateField(uiManager, "questionText", questionText);
            SetPrivateField(uiManager, "modeBadgeText", modeBadge);
            SetPrivateField(uiManager, "levelBadgeText", levelBadge);
            SetPrivateField(uiManager, "scoreText", scoreBadge);
            SetPrivateField(uiManager, "streakText", streakBadge);
            SetPrivateField(uiManager, "choiceButtons", choiceBtns);
            SetPrivateField(uiManager, "choiceButtonTexts", choiceTexts);
            SetPrivateField(uiManager, "choiceButtonBackgrounds", choiceBgs);
            SetPrivateField(uiManager, "playerHealthSlider", playerSlider);
            SetPrivateField(uiManager, "playerHealthText", playerHpText);
            SetPrivateField(uiManager, "enemyHealthSlider", enemySlider);
            SetPrivateField(uiManager, "enemyHealthText", enemyHpText);
            SetPrivateField(uiManager, "enemyNameText", enemyName);
            SetPrivateField(uiManager, "turnTimerSlider", timerSlider);
            SetPrivateField(uiManager, "postLevelTitleText", postLevelTitle);
            SetPrivateField(uiManager, "postLevelStatsText", postLevelStats);
            SetPrivateField(uiManager, "gameOverScoreText", gameOverScore);
            SetPrivateField(uiManager, "gameOverLevelText", gameOverLevel);
            SetPrivateField(uiManager, "notificationContainer", notificationContainer);
            SetPrivateField(uiManager, "notificationText", notifText);

            EditorUtility.SetDirty(gameManager);
            EditorUtility.SetDirty(duelController);
            EditorUtility.SetDirty(uiManager);
            EditorUtility.SetDirty(canvasGo);

            Debug.Log("<color=green>[DigitDuelist]</color> Complete Canvas UI hierarchy and Game Managers successfully created and wired!");
            EditorUtility.DisplayDialog("Digit Duelist Setup Complete", "The UI Canvas hierarchy and buttons have been updated with Unity 6 Input System support and direct button action components!", "Great!");
        }

        #region Helper Creators

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                eventSystem = esGo.AddComponent<EventSystem>();
                Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
            }

            GameObject go = eventSystem.gameObject;

            // In Unity 6 with com.unity.inputsystem, InputSystemUIInputModule is required
            Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem")
                ?? Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem.ForUI")
                ?? Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, UnityEngine.UI");

            if (inputSystemModuleType != null)
            {
                StandaloneInputModule standalone = go.GetComponent<StandaloneInputModule>();
                if (standalone != null)
                {
                    Undo.DestroyObjectImmediate(standalone);
                }

                if (go.GetComponent(inputSystemModuleType) == null)
                {
                    Undo.AddComponent(go, inputSystemModuleType);
                }
            }
            else
            {
                if (go.GetComponent<StandaloneInputModule>() == null && go.GetComponent<BaseInputModule>() == null)
                {
                    Undo.AddComponent<StandaloneInputModule>(go);
                }
            }
        }

        private static GameObject CreatePanel(string name, Transform parent, Color bgColor, bool shouldBlockRaycast, Vector2? anchor = null, Vector2? size = null)
        {
            // Remove existing duplicate panel if re-running
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            if (anchor.HasValue && size.HasValue)
            {
                rect.anchorMin = anchor.Value;
                rect.anchorMax = anchor.Value;
                rect.pivot = anchor.Value;
                rect.sizeDelta = size.Value;
            }
            else
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            if (shouldBlockRaycast || bgColor.a > 0.01f)
            {
                Image image = panel.AddComponent<Image>();
                image.color = bgColor;
                image.raycastTarget = shouldBlockRaycast;
            }

            Undo.RegisterCreatedObjectUndo(panel, $"Create {name}");
            return panel;
        }

        private static TextMeshProUGUI CreateTitle(Transform parent, string text, Vector2 anchoredPos, float fontSize, Color color)
        {
            GameObject go = new GameObject("TitleText");
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(800, 100);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false;

            return tmp;
        }

        private static TextMeshProUGUI CreateSubtitle(Transform parent, string text, Vector2 anchoredPos, float fontSize)
        {
            GameObject go = new GameObject("SubtitleText");
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(800, 80);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.85f, 0.9f, 0.95f, 0.85f);
            tmp.raycastTarget = false;

            return tmp;
        }

        private static TextMeshProUGUI CreateBadge(Transform parent, string text, Vector2 anchoredPos, float fontSize, TextAlignmentOptions align, Color? color = null)
        {
            GameObject go = new GameObject("BadgeText");
            go.transform.SetParent(parent, false);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(400, 50);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = align;
            tmp.color = color ?? Color.white;
            tmp.raycastTarget = false;

            return tmp;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, ButtonActionType actionType, Color? bgColor = null)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);

            RectTransform rect = btnGo.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image image = btnGo.AddComponent<Image>();
            image.color = bgColor ?? new Color(0.2f, 0.35f, 0.55f, 1f);
            image.raycastTarget = true;

            Button button = btnGo.AddComponent<Button>();
            ColorBlock cb = button.colors;
            cb.highlightedColor = image.color * 1.25f;
            cb.pressedColor = image.color * 0.85f;
            button.colors = cb;

            // Attach DigitDuelistButtonAction for guaranteed runtime execution
            DigitDuelistButtonAction actionComp = btnGo.AddComponent<DigitDuelistButtonAction>();
            actionComp.ActionType = actionType;

            // Label
            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);

            RectTransform labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 24;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false; // Never block raycast on button label

            return btnGo;
        }

        private static Slider CreateHealthBar(Transform parent, string name, Vector2 pos, Color fillColor, Vector2? size = null)
        {
            GameObject sliderGo = new GameObject(name);
            sliderGo.transform.SetParent(parent, false);

            RectTransform rect = sliderGo.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size ?? new Vector2(300, 26);

            Slider slider = sliderGo.AddComponent<Slider>();

            // Background
            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            RectTransform bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.12f, 0.16f, 0.85f);
            bgImg.raycastTarget = false;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGo.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            // Fill
            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImg = fill.AddComponent<Image>();
            fillImg.color = fillColor;
            fillImg.raycastTarget = false;

            slider.fillRect = fillRect;
            slider.targetGraphic = fillImg;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 100f;

            return slider;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fi != null)
            {
                fi.SetValue(target, value);
            }
        }

        #endregion
    }
}
#endif

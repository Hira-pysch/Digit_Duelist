#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DigitDuelist.Editor
{
    public class DigitDuelistKidsSetupEditor : EditorWindow
    {
        [MenuItem("Tools/Digit Duelist/Upgrade to Kids Edition (Spells, Zen Mode & Hints)", false, 3)]
        public static void UpgradeToKidsEdition()
        {
            // 1. Ensure Managers exist
            GameObject managersGo = GameObject.Find("[GameManagers]");
            if (managersGo == null)
            {
                managersGo = new GameObject("[GameManagers]");
                Undo.RegisterCreatedObjectUndo(managersGo, "Create GameManagers");
            }

            KidAudioManager audioMgr = managersGo.GetComponent<KidAudioManager>() ?? Undo.AddComponent<KidAudioManager>(managersGo);
            GameModeManager modeMgr = managersGo.GetComponent<GameModeManager>() ?? Undo.AddComponent<GameModeManager>(managersGo);
            AssistSpellsController spellsCtrl = managersGo.GetComponent<AssistSpellsController>() ?? Undo.AddComponent<AssistSpellsController>(managersGo);
            KidFeedbackManager feedbackMgr = managersGo.GetComponent<KidFeedbackManager>() ?? Undo.AddComponent<KidFeedbackManager>(managersGo);
            SkinCustomizationManager skinMgr = managersGo.GetComponent<SkinCustomizationManager>() ?? Undo.AddComponent<SkinCustomizationManager>(managersGo);
            LevelUpgradeManager upgradeMgr = managersGo.GetComponent<LevelUpgradeManager>() ?? Undo.AddComponent<LevelUpgradeManager>(managersGo);
            BossChoiceFlipper flipper = managersGo.GetComponent<BossChoiceFlipper>() ?? Undo.AddComponent<BossChoiceFlipper>(managersGo);
            StageBackgroundManager bgMgr = managersGo.GetComponent<StageBackgroundManager>() ?? Undo.AddComponent<StageBackgroundManager>(managersGo);
            if (bgMgr != null)
            {
                bgMgr.EnsureMaterialsLoaded();
                bgMgr.EnsureBackdropQuad();
                EditorUtility.SetDirty(bgMgr);
            }

            // 2. Find Canvas
            GameObject canvasGo = GameObject.Find("DigitDuelist_Canvas");
            if (canvasGo == null)
            {
                EditorUtility.DisplayDialog("Error", "Please run 'Tools > Digit Duelist > Create Complete UI Canvas & Managers' first!", "OK");
                return;
            }

            Transform canvasRoot = canvasGo.transform;
            Transform gameplayPanel = canvasRoot.Find("Gameplay_Panel");
            Transform mainMenuPanel = canvasRoot.Find("MainMenu_Panel");
            Transform gameModePanel = canvasRoot.Find("GameMode_Panel");
            Transform gameOverPanel = canvasRoot.Find("GameOver_Panel");

            // 3. Add Zen Mode Toggle on Main Menu
            if (mainMenuPanel != null && mainMenuPanel.Find("Btn_ZenToggle") == null)
            {
                CreateButton(mainMenuPanel, "Btn_ZenToggle", "🧘 ZEN MODE (UNTIMED)", new Vector2(0, -270), new Vector2(340, 50), ButtonActionType.ToggleZenMode, new Color(0.2f, 0.6f, 0.7f));
            }

            // 4. Add Age-Based Presets on Mode Selection
            if (gameModePanel != null && gameModePanel.Find("PresetContainer") == null)
            {
                GameObject presetContainer = new GameObject("PresetContainer");
                presetContainer.transform.SetParent(gameModePanel, false);
                RectTransform rt = presetContainer.AddComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, 120);
                rt.sizeDelta = new Vector2(800, 60);

                CreateButton(presetContainer.transform, "Btn_Preset_Novice", "🌱 Ages 5-7 (Novice)", new Vector2(-260, 0), new Vector2(230, 48), ButtonActionType.SetPresetNovice, new Color(0.2f, 0.7f, 0.4f));
                CreateButton(presetContainer.transform, "Btn_Preset_Adventurer", "⚔️ Ages 8-9 (Adventurer)", new Vector2(0, 0), new Vector2(240, 48), ButtonActionType.SetPresetAdventurer, new Color(0.2f, 0.5f, 0.85f));
                CreateButton(presetContainer.transform, "Btn_Preset_Master", "👑 Ages 10+ (Master)", new Vector2(260, 0), new Vector2(230, 48), ButtonActionType.SetPresetMaster, new Color(0.85f, 0.5f, 0.15f));
            }

            // 5. Add Kid-Friendly Spells on Gameplay HUD
            if (gameplayPanel != null && gameplayPanel.Find("Spells_Container") == null)
            {
                GameObject spellsContainer = new GameObject("Spells_Container");
                spellsContainer.transform.SetParent(gameplayPanel, false);
                RectTransform rt = spellsContainer.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0, 50);
                rt.sizeDelta = new Vector2(700, 60);

                CreateButton(spellsContainer.transform, "Btn_Spell_Freeze", "❄️ Freeze Time [Q]", new Vector2(-220, 0), new Vector2(190, 50), ButtonActionType.CastFreeze, new Color(0.25f, 0.7f, 0.95f));
                CreateButton(spellsContainer.transform, "Btn_Spell_5050", "🔮 Crystal 50/50 [W]", new Vector2(0, 0), new Vector2(200, 50), ButtonActionType.CastCrystalBall, new Color(0.7f, 0.35f, 0.95f));
                CreateButton(spellsContainer.transform, "Btn_Spell_Shield", "🛡️ Bubble Shield [E]", new Vector2(220, 0), new Vector2(190, 50), ButtonActionType.CastBubbleShield, new Color(0.2f, 0.85f, 0.5f));
            }

            // 6. Add In-Game Exit Button on Gameplay HUD
            if (gameplayPanel != null && gameplayPanel.Find("Btn_HUD_Exit") == null)
            {
                CreateButton(gameplayPanel, "Btn_HUD_Exit", "🚪 EXIT", new Vector2(620, 480), new Vector2(160, 50), ButtonActionType.Choice0, new Color(0.72f, 0.22f, 0.22f));
            }

            // 7. Add Visual Math Hint Card on Gameplay Panel
            if (gameplayPanel != null && gameplayPanel.Find("VisualMathHintCard") == null)
            {
                GameObject hintCardGo = new GameObject("VisualMathHintCard");
                hintCardGo.transform.SetParent(gameplayPanel, false);
                RectTransform rt = hintCardGo.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 40);
                rt.sizeDelta = new Vector2(720, 420);

                Image bg = hintCardGo.AddComponent<Image>();
                bg.color = new Color(0.12f, 0.16f, 0.25f, 0.98f);

                // Title
                GameObject titleGo = new GameObject("TitleText");
                titleGo.transform.SetParent(hintCardGo.transform, false);
                RectTransform titleRt = titleGo.AddComponent<RectTransform>();
                titleRt.anchoredPosition = new Vector2(0, 160);
                titleRt.sizeDelta = new Vector2(680, 50);
                TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
                titleTmp.text = "💡 Math Detective Hint!";
                titleTmp.fontSize = 34;
                titleTmp.alignment = TextAlignmentOptions.Center;
                titleTmp.fontStyle = FontStyles.Bold;
                titleTmp.color = new Color(1f, 0.88f, 0.3f);

                // Visual Grid
                GameObject gridGo = new GameObject("VisualGridText");
                gridGo.transform.SetParent(hintCardGo.transform, false);
                RectTransform gridRt = gridGo.AddComponent<RectTransform>();
                gridRt.anchoredPosition = new Vector2(0, 40);
                gridRt.sizeDelta = new Vector2(660, 160);
                TextMeshProUGUI gridTmp = gridGo.AddComponent<TextMeshProUGUI>();
                gridTmp.text = "⭐ ⭐ ⭐ ⭐\n⭐ ⭐ ⭐ ⭐\n⭐ ⭐ ⭐ ⭐";
                gridTmp.fontSize = 28;
                gridTmp.alignment = TextAlignmentOptions.Center;
                gridTmp.color = Color.white;

                // Explanation
                GameObject expGo = new GameObject("ExplanationText");
                expGo.transform.SetParent(hintCardGo.transform, false);
                RectTransform expRt = expGo.AddComponent<RectTransform>();
                expRt.anchoredPosition = new Vector2(0, -90);
                expRt.sizeDelta = new Vector2(660, 80);
                TextMeshProUGUI expTmp = expGo.AddComponent<TextMeshProUGUI>();
                expTmp.text = "3 groups of 4 = 12 total stars!";
                expTmp.fontSize = 24;
                expTmp.alignment = TextAlignmentOptions.Center;
                expTmp.color = new Color(0.85f, 0.95f, 1f);

                // Dismiss Button
                GameObject btnGo = CreateButton(hintCardGo.transform, "Btn_DismissHint", "GOT IT! 👍", new Vector2(0, -165), new Vector2(240, 50), ButtonActionType.DismissHint, new Color(0.2f, 0.75f, 0.35f));
                Button dismissBtn = btnGo.GetComponent<Button>();

                VisualMathHintCard hintCardComp = hintCardGo.AddComponent<VisualMathHintCard>();
                SetPrivateField(hintCardComp, "cardRoot", hintCardGo);
                SetPrivateField(hintCardComp, "titleText", titleTmp);
                SetPrivateField(hintCardComp, "visualGridText", gridTmp);
                SetPrivateField(hintCardComp, "explanationText", expTmp);
                SetPrivateField(hintCardComp, "dismissButton", dismissBtn);

                SetPrivateField(feedbackMgr, "hintCard", hintCardComp);

                hintCardGo.SetActive(false); // Hide initially
            }

            // 8. Create 5-Level Upgrade Modal Panel
            Transform existingUpgradePanel = canvasRoot.Find("UpgradeSelection_Panel");
            if (existingUpgradePanel == null)
            {
                GameObject upgradePanelGo = new GameObject("UpgradeSelection_Panel");
                upgradePanelGo.transform.SetParent(canvasRoot, false);
                RectTransform rt = upgradePanelGo.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;

                Image bg = upgradePanelGo.AddComponent<Image>();
                bg.color = new Color(0.04f, 0.07f, 0.12f, 0.95f);

                CanvasGroup cg = upgradePanelGo.AddComponent<CanvasGroup>();

                // Container
                GameObject containerGo = new GameObject("CardContainer");
                containerGo.transform.SetParent(upgradePanelGo.transform, false);
                RectTransform containerRt = containerGo.AddComponent<RectTransform>();
                containerRt.anchoredPosition = Vector2.zero;
                containerRt.sizeDelta = new Vector2(1000, 600);

                // Title
                GameObject titleGo = new GameObject("TitleText");
                titleGo.transform.SetParent(containerGo.transform, false);
                RectTransform titleRt = titleGo.AddComponent<RectTransform>();
                titleRt.anchoredPosition = new Vector2(0, 200);
                titleRt.sizeDelta = new Vector2(900, 60);
                TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
                titleTmp.text = "🏆 MILESTONE REWARD!";
                titleTmp.fontSize = 52;
                titleTmp.alignment = TextAlignmentOptions.Center;
                titleTmp.fontStyle = FontStyles.Bold;
                titleTmp.color = new Color(1f, 0.88f, 0.25f);

                // Subtitle
                GameObject subGo = new GameObject("SubtitleText");
                subGo.transform.SetParent(containerGo.transform, false);
                RectTransform subRt = subGo.AddComponent<RectTransform>();
                subRt.anchoredPosition = new Vector2(0, 140);
                subRt.sizeDelta = new Vector2(900, 40);
                TextMeshProUGUI subTmp = subGo.AddComponent<TextMeshProUGUI>();
                subTmp.text = "Choose 1 Permanent Stat Buff for your hero:";
                subTmp.fontSize = 26;
                subTmp.alignment = TextAlignmentOptions.Center;
                subTmp.color = new Color(0.8f, 0.92f, 1f);

                Button[] optionButtons = new Button[3];
                TextMeshProUGUI[] optionTitles = new TextMeshProUGUI[3];
                TextMeshProUGUI[] optionDescs = new TextMeshProUGUI[3];
                TextMeshProUGUI[] optionBadges = new TextMeshProUGUI[3];
                Image[] optionBgs = new Image[3];

                string[] titles = { "⚔️ ATTACK BOOST", "💖 HEALTH BOOST", "🛡️ DEFENSE BOOST" };
                string[] descs = { "Increase strike damage on correct answers.", "Expand max vitality and restore full health.", "Fortify armor to absorb incoming mistakes." };
                string[] badges = { "+10 ATK", "+30 HP", "+5 DEF" };
                Color[] colors = { new Color(0.9f, 0.35f, 0.25f), new Color(0.2f, 0.75f, 0.35f), new Color(0.2f, 0.6f, 0.95f) };
                float[] xOffsets = { -310f, 0f, 310f };

                for (int i = 0; i < 3; i++)
                {
                    GameObject cardGo = new GameObject($"UpgradeCard_{i}");
                    cardGo.transform.SetParent(containerGo.transform, false);
                    RectTransform cardRt = cardGo.AddComponent<RectTransform>();
                    cardRt.anchoredPosition = new Vector2(xOffsets[i], -30);
                    cardRt.sizeDelta = new Vector2(280, 320);

                    Image cardImg = cardGo.AddComponent<Image>();
                    cardImg.color = colors[i] * 0.35f;
                    optionBgs[i] = cardImg;

                    // Card Title
                    GameObject cTitleGo = new GameObject("CardTitle");
                    cTitleGo.transform.SetParent(cardGo.transform, false);
                    RectTransform cTitleRt = cTitleGo.AddComponent<RectTransform>();
                    cTitleRt.anchoredPosition = new Vector2(0, 110);
                    cTitleRt.sizeDelta = new Vector2(260, 45);
                    TextMeshProUGUI cTitleTmp = cTitleGo.AddComponent<TextMeshProUGUI>();
                    cTitleTmp.text = titles[i];
                    cTitleTmp.fontSize = 24;
                    cTitleTmp.fontStyle = FontStyles.Bold;
                    cTitleTmp.alignment = TextAlignmentOptions.Center;
                    cTitleTmp.color = Color.white;
                    optionTitles[i] = cTitleTmp;

                    // Card Desc
                    GameObject cDescGo = new GameObject("CardDesc");
                    cDescGo.transform.SetParent(cardGo.transform, false);
                    RectTransform cDescRt = cDescGo.AddComponent<RectTransform>();
                    cDescRt.anchoredPosition = new Vector2(0, 30);
                    cDescRt.sizeDelta = new Vector2(250, 90);
                    TextMeshProUGUI cDescTmp = cDescGo.AddComponent<TextMeshProUGUI>();
                    cDescTmp.text = descs[i];
                    cDescTmp.fontSize = 18;
                    cDescTmp.alignment = TextAlignmentOptions.Center;
                    cDescTmp.color = new Color(0.9f, 0.95f, 1f);
                    optionDescs[i] = cDescTmp;

                    // Card Badge
                    GameObject badgeGo = new GameObject("CardBadge");
                    badgeGo.transform.SetParent(cardGo.transform, false);
                    RectTransform badgeRt = badgeGo.AddComponent<RectTransform>();
                    badgeRt.anchoredPosition = new Vector2(0, -40);
                    badgeRt.sizeDelta = new Vector2(200, 40);
                    TextMeshProUGUI badgeTmp = badgeGo.AddComponent<TextMeshProUGUI>();
                    badgeTmp.text = badges[i];
                    badgeTmp.fontSize = 28;
                    badgeTmp.fontStyle = FontStyles.Bold;
                    badgeTmp.alignment = TextAlignmentOptions.Center;
                    badgeTmp.color = new Color(1f, 0.95f, 0.3f);
                    optionBadges[i] = badgeTmp;

                    // Select Button
                    GameObject btnGo = CreateButton(cardGo.transform, $"Btn_SelectUpgrade_{i}", "CHOOSE", new Vector2(0, -110), new Vector2(220, 50), ButtonActionType.Choice0, colors[i]);
                    Button b = btnGo.GetComponent<Button>();
                    optionButtons[i] = b;
                }

                UpgradeSelectionUI uiComp = upgradePanelGo.AddComponent<UpgradeSelectionUI>();
                SetPrivateField(uiComp, "modalRoot", upgradePanelGo);
                SetPrivateField(uiComp, "canvasGroup", cg);
                SetPrivateField(uiComp, "cardContainer", containerRt);
                SetPrivateField(uiComp, "headerTitleText", titleTmp);
                SetPrivateField(uiComp, "headerSubtitleText", subTmp);
                SetPrivateField(uiComp, "optionButtons", optionButtons);
                SetPrivateField(uiComp, "optionTitleTexts", optionTitles);
                SetPrivateField(uiComp, "optionDescTexts", optionDescs);
                SetPrivateField(uiComp, "optionBadgeTexts", optionBadges);
                SetPrivateField(uiComp, "optionBgImages", optionBgs);

                SetPrivateField(upgradeMgr, "upgradeUI", uiComp);

                upgradePanelGo.SetActive(false);
            }

            // Mark dirty
            EditorUtility.SetDirty(managersGo);
            if (canvasGo != null) EditorUtility.SetDirty(canvasGo);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("<color=green>[DigitDuelist]</color> Kids Edition features successfully added and configured!");
            EditorUtility.DisplayDialog(
                "Duelist Math - Kids & Boss Edition Ready!",
                "All systems have been installed and wired:\n\n" +
                "• 🏆 5-Level Upgrade System (Attack, Health, Defense buffs every 5 levels)\n" +
                "• 👑 Level 5 Boss Disruption (Higher Boss HP & dynamic UI choice flipping)\n" +
                "• 🧘 Zen / Untimed Mode toggle\n" +
                "• ⌨️ Keyboard numeric shortcuts (1, 2, 3, 4)\n" +
                "• ❄️ Freeze, 🔮 50/50, and 🛡️ Bubble Shield assist spells\n" +
                "• 💡 Visual Math Detective Hint card\n" +
                "• 🎵 Whimsical procedural chime/boing/fanfare audio\n" +
                "• 🌟 Growth-mindset encouragement & dizzy star cartoon states\n" +
                "• 🌱 Age presets: Novice (5-7), Adventurer (8-9), Master (10+)\n" +
                "• 🎨 Milestone armor/skin unlocking system",
                "Fantastic!"
            );
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, ButtonActionType action, Color? color = null)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);

            RectTransform rt = btnGo.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnGo.AddComponent<Image>();
            img.color = color ?? new Color(0.18f, 0.28f, 0.45f);

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = img.color * 1.2f;
            cb.pressedColor = img.color * 0.8f;
            btn.colors = cb;

            DigitDuelistButtonAction actionComp = btnGo.AddComponent<DigitDuelistButtonAction>();
            actionComp.ActionType = action;

            // Label
            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            RectTransform labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = Mathf.Clamp(Mathf.RoundToInt(size.y * 0.44f), 14, 30);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.raycastTarget = false;

            return btnGo;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
#endif

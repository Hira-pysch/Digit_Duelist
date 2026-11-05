using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DigitDuelist
{
    /// <summary>
    /// Displays the 3-choice upgrade modal when reaching a 5-level milestone.
    /// Features smooth scale-up animation and visual preview of permanent stat boosts.
    /// Auto-generates its UI hierarchy at runtime if missing from the scene.
    /// </summary>
    public class UpgradeSelectionUI : MonoBehaviour
    {
        public static UpgradeSelectionUI Instance { get; private set; }

        [Header("Root & Backdrop")]
        [SerializeField] private GameObject modalRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform cardContainer;

        [Header("Header Text")]
        [SerializeField] private TextMeshProUGUI headerTitleText;
        [SerializeField] private TextMeshProUGUI headerSubtitleText;

        [Header("3 Upgrade Option Cards")]
        [SerializeField] private Button[] optionButtons = new Button[3];
        [SerializeField] private TextMeshProUGUI[] optionTitleTexts = new TextMeshProUGUI[3];
        [SerializeField] private TextMeshProUGUI[] optionDescTexts = new TextMeshProUGUI[3];
        [SerializeField] private TextMeshProUGUI[] optionBadgeTexts = new TextMeshProUGUI[3];
        [SerializeField] private Image[] optionBgImages = new Image[3];

        private Action<UpgradeBuffType> onSelectionMade;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (modalRoot == null) modalRoot = gameObject;
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? modalRoot.AddComponent<CanvasGroup>();
            modalRoot.SetActive(false);
        }

        /// <summary>
        /// Guarantees that an UpgradeSelectionUI instance exists in the scene, building one dynamically if needed.
        /// </summary>
        public static UpgradeSelectionUI EnsureInstance()
        {
            if (Instance != null) return Instance;

            UpgradeSelectionUI existing = FindFirstObjectByType<UpgradeSelectionUI>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            // Find canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("DigitDuelist_Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            GameObject panelGo = new GameObject("UpgradeSelection_Panel");
            panelGo.transform.SetParent(canvas.transform, false);

            RectTransform rootRt = panelGo.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            Image bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.07f, 0.14f, 0.96f);

            CanvasGroup cg = panelGo.AddComponent<CanvasGroup>();

            // Container
            GameObject containerGo = new GameObject("CardContainer");
            containerGo.transform.SetParent(panelGo.transform, false);
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
            titleTmp.fontSize = 50;
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

            Button[] optionBtns = new Button[3];
            TextMeshProUGUI[] optionTitles = new TextMeshProUGUI[3];
            TextMeshProUGUI[] optionDescs = new TextMeshProUGUI[3];
            TextMeshProUGUI[] optionBadges = new TextMeshProUGUI[3];
            Image[] optionBgs = new Image[3];

            float[] xOffsets = { -310f, 0f, 310f };

            for (int i = 0; i < 3; i++)
            {
                GameObject cardGo = new GameObject($"UpgradeCard_{i}");
                cardGo.transform.SetParent(containerGo.transform, false);
                RectTransform cardRt = cardGo.AddComponent<RectTransform>();
                cardRt.anchoredPosition = new Vector2(xOffsets[i], -30);
                cardRt.sizeDelta = new Vector2(280, 320);

                Image cardImg = cardGo.AddComponent<Image>();
                cardImg.color = new Color(0.12f, 0.18f, 0.3f, 0.95f);
                optionBgs[i] = cardImg;

                // Card Title
                GameObject cTitleGo = new GameObject("CardTitle");
                cTitleGo.transform.SetParent(cardGo.transform, false);
                RectTransform cTitleRt = cTitleGo.AddComponent<RectTransform>();
                cTitleRt.anchoredPosition = new Vector2(0, 110);
                cTitleRt.sizeDelta = new Vector2(260, 45);
                TextMeshProUGUI cTitleTmp = cTitleGo.AddComponent<TextMeshProUGUI>();
                cTitleTmp.text = "UPGRADE";
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
                cDescTmp.text = "Description";
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
                badgeTmp.text = "+BONUS";
                badgeTmp.fontSize = 28;
                badgeTmp.fontStyle = FontStyles.Bold;
                badgeTmp.alignment = TextAlignmentOptions.Center;
                badgeTmp.color = new Color(1f, 0.95f, 0.3f);
                optionBadges[i] = badgeTmp;

                // Select Button
                GameObject btnGo = new GameObject($"Btn_SelectUpgrade_{i}");
                btnGo.transform.SetParent(cardGo.transform, false);
                RectTransform btnRt = btnGo.AddComponent<RectTransform>();
                btnRt.anchoredPosition = new Vector2(0, -110);
                btnRt.sizeDelta = new Vector2(220, 50);

                Image btnImg = btnGo.AddComponent<Image>();
                btnImg.color = new Color(0.2f, 0.65f, 0.35f);

                Button btn = btnGo.AddComponent<Button>();
                optionBtns[i] = btn;

                GameObject btnLabel = new GameObject("Label");
                btnLabel.transform.SetParent(btnGo.transform, false);
                RectTransform lblRt = btnLabel.AddComponent<RectTransform>();
                lblRt.anchorMin = Vector2.zero;
                lblRt.anchorMax = Vector2.one;
                lblRt.sizeDelta = Vector2.zero;

                TextMeshProUGUI lblTmp = btnLabel.AddComponent<TextMeshProUGUI>();
                lblTmp.text = "CHOOSE";
                lblTmp.fontSize = 22;
                lblTmp.fontStyle = FontStyles.Bold;
                lblTmp.alignment = TextAlignmentOptions.Center;
                lblTmp.color = Color.white;
                lblTmp.raycastTarget = false;
            }

            UpgradeSelectionUI ui = panelGo.AddComponent<UpgradeSelectionUI>();
            ui.modalRoot = panelGo;
            ui.canvasGroup = cg;
            ui.cardContainer = containerRt;
            ui.headerTitleText = titleTmp;
            ui.headerSubtitleText = subTmp;
            ui.optionButtons = optionBtns;
            ui.optionTitleTexts = optionTitles;
            ui.optionDescTexts = optionDescs;
            ui.optionBadgeTexts = optionBadges;
            ui.optionBgImages = optionBgs;

            Instance = ui;
            panelGo.SetActive(false);
            return ui;
        }

        /// <summary>
        /// Populates and animates the 3-choice upgrade prompt.
        /// </summary>
        public void DisplayUpgradePrompt(UpgradeBuffOption[] options, Action<UpgradeBuffType> callback)
        {
            if (modalRoot == null) modalRoot = gameObject;
            onSelectionMade = callback;
            modalRoot.SetActive(true);

            if (headerTitleText != null) headerTitleText.text = "🏆 MILESTONE REWARD!";
            if (headerSubtitleText != null) headerSubtitleText.text = "Choose 1 Permanent Stat Buff for your hero:";

            for (int i = 0; i < options.Length && i < optionButtons.Length; i++)
            {
                int index = i;
                UpgradeBuffOption opt = options[i];

                if (optionTitleTexts != null && i < optionTitleTexts.Length && optionTitleTexts[i] != null)
                {
                    optionTitleTexts[i].text = opt.title;
                }

                if (optionDescTexts != null && i < optionDescTexts.Length && optionDescTexts[i] != null)
                {
                    optionDescTexts[i].text = opt.description;
                }

                if (optionBadgeTexts != null && i < optionBadgeTexts.Length && optionBadgeTexts[i] != null)
                {
                    optionBadgeTexts[i].text = opt.bonusValueText;
                }

                if (optionBgImages != null && i < optionBgImages.Length && optionBgImages[i] != null)
                {
                    optionBgImages[i].color = opt.themeColor * 0.35f;
                }

                if (optionButtons != null && i < optionButtons.Length && optionButtons[i] != null)
                {
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionClicked(opt.type));
                }
            }

            StopAllCoroutines();
            StartCoroutine(AnimateOpenRoutine());
        }

        private void OnOptionClicked(UpgradeBuffType type)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateCloseRoutine(() =>
            {
                if (modalRoot != null) modalRoot.SetActive(false);
                onSelectionMade?.Invoke(type);
            }));
        }

        private IEnumerator AnimateOpenRoutine()
        {
            float elapsed = 0f;
            float duration = 0.35f;

            if (cardContainer != null) cardContainer.localScale = Vector3.one * 0.7f;
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float easeOutBack = Mathf.Sin(t * Mathf.PI * 0.5f);

                if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t * 1.5f);
                if (cardContainer != null)
                {
                    float scale = Mathf.Lerp(0.7f, 1f, easeOutBack);
                    cardContainer.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (cardContainer != null) cardContainer.localScale = Vector3.one;
        }

        private IEnumerator AnimateCloseRoutine(Action onComplete)
        {
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                if (canvasGroup != null) canvasGroup.alpha = 1f - t;
                if (cardContainer != null) cardContainer.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, t);

                yield return null;
            }

            onComplete?.Invoke();
        }
    }
}

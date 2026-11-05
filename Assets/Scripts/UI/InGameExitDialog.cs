using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace DigitDuelist
{
    /// <summary>
    /// In-game Exit Confirmation Dialog.
    /// Allows the player to pause combat and choose between:
    /// 1. Save and Quit to Main Menu
    /// 2. Quit to Main Menu without saving
    /// 3. Resume combat
    /// Auto-constructs its UI dynamically at runtime if not present in the scene.
    /// </summary>
    public class InGameExitDialog : MonoBehaviour
    {
        public static InGameExitDialog Instance { get; private set; }

        [Header("Dialog Elements")]
        [SerializeField] private GameObject dialogRoot;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RectTransform cardContainer;

        [Header("Buttons")]
        [SerializeField] private Button saveAndQuitButton;
        [SerializeField] private Button quitWithoutSavingButton;
        [SerializeField] private Button cancelButton;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (dialogRoot == null) dialogRoot = gameObject;
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? dialogRoot.AddComponent<CanvasGroup>();

            WireButtons();
            dialogRoot.SetActive(false);
        }

        private void WireButtons()
        {
            if (saveAndQuitButton != null)
            {
                saveAndQuitButton.onClick.RemoveAllListeners();
                saveAndQuitButton.onClick.AddListener(OnSaveAndQuitClicked);
            }

            if (quitWithoutSavingButton != null)
            {
                quitWithoutSavingButton.onClick.RemoveAllListeners();
                quitWithoutSavingButton.onClick.AddListener(OnQuitWithoutSavingClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        /// <summary>
        /// Guarantees that an InGameExitDialog instance exists in the scene, generating one dynamically if needed.
        /// </summary>
        public static InGameExitDialog EnsureInstance()
        {
            if (Instance != null) return Instance;

            InGameExitDialog existing = FindFirstObjectByType<InGameExitDialog>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGo = new GameObject("DigitDuelist_Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            GameObject panelGo = new GameObject("InGameExit_Dialog");
            panelGo.transform.SetParent(canvas.transform, false);

            RectTransform rootRt = panelGo.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.sizeDelta = Vector2.zero;

            Image bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.05f, 0.09f, 0.94f);

            CanvasGroup cg = panelGo.AddComponent<CanvasGroup>();

            // Modal Box
            GameObject boxGo = new GameObject("ModalBox");
            boxGo.transform.SetParent(panelGo.transform, false);
            RectTransform boxRt = boxGo.AddComponent<RectTransform>();
            boxRt.anchoredPosition = Vector2.zero;
            boxRt.sizeDelta = new Vector2(560, 420);

            Image boxBg = boxGo.AddComponent<Image>();
            boxBg.color = new Color(0.1f, 0.14f, 0.22f, 0.98f);

            // Title
            GameObject titleGo = new GameObject("TitleText");
            titleGo.transform.SetParent(boxGo.transform, false);
            RectTransform titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 140);
            titleRt.sizeDelta = new Vector2(520, 50);
            TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "🚪 LEAVE DUEL?";
            titleTmp.fontSize = 36;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = new Color(1f, 0.85f, 0.3f);

            // Subtitle
            GameObject subGo = new GameObject("SubtitleText");
            subGo.transform.SetParent(boxGo.transform, false);
            RectTransform subRt = subGo.AddComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0, 85);
            subRt.sizeDelta = new Vector2(500, 50);
            TextMeshProUGUI subTmp = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = "Would you like to save your progress before returning to the main menu?";
            subTmp.fontSize = 20;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.85f, 0.92f, 1f);

            // Button 1: Save & Quit
            GameObject btnSaveGo = CreateDialogButton(boxGo.transform, "Btn_Exit_SaveQuit", "💾 SAVE & QUIT TO MENU", new Vector2(0, 10), new Vector2(440, 55), new Color(0.18f, 0.65f, 0.35f));
            Button btnSave = btnSaveGo.GetComponent<Button>();

            // Button 2: Quit Without Saving
            GameObject btnQuitGo = CreateDialogButton(boxGo.transform, "Btn_Exit_QuitNoSave", "🚪 QUIT WITHOUT SAVING", new Vector2(0, -60), new Vector2(440, 55), new Color(0.75f, 0.25f, 0.25f));
            Button btnQuit = btnQuitGo.GetComponent<Button>();

            // Button 3: Resume / Cancel
            GameObject btnCancelGo = CreateDialogButton(boxGo.transform, "Btn_Exit_Cancel", "RESUME DUEL", new Vector2(0, -135), new Vector2(320, 48), new Color(0.35f, 0.4f, 0.5f));
            Button btnCancel = btnCancelGo.GetComponent<Button>();

            InGameExitDialog dialog = panelGo.AddComponent<InGameExitDialog>();
            dialog.dialogRoot = panelGo;
            dialog.canvasGroup = cg;
            dialog.cardContainer = boxRt;
            dialog.saveAndQuitButton = btnSave;
            dialog.quitWithoutSavingButton = btnQuit;
            dialog.cancelButton = btnCancel;

            dialog.WireButtons();
            Instance = dialog;
            panelGo.SetActive(false);
            return dialog;
        }

        private static GameObject CreateDialogButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);

            RectTransform rt = btnGo.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = btnGo.AddComponent<Image>();
            img.color = color;

            Button btn = btnGo.AddComponent<Button>();

            GameObject lblGo = new GameObject("Label");
            lblGo.transform.SetParent(btnGo.transform, false);
            RectTransform lblRt = lblGo.AddComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero;
            lblRt.anchorMax = Vector2.one;
            lblRt.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;

            return btnGo;
        }

        public void ShowDialog()
        {
            if (dialogRoot == null) dialogRoot = gameObject;
            dialogRoot.SetActive(true);
            Time.timeScale = 0f; // Freeze game simulation

            StopAllCoroutines();
            StartCoroutine(AnimateOpenRoutine());
        }

        public void HideDialog()
        {
            StopAllCoroutines();
            StartCoroutine(AnimateCloseRoutine(() =>
            {
                if (dialogRoot != null) dialogRoot.SetActive(false);
                Time.timeScale = 1f; // Unfreeze game
            }));
        }

        private void OnSaveAndQuitClicked()
        {
            Time.timeScale = 1f;
            if (dialogRoot != null) dialogRoot.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SaveCurrentGame();
                GameManager.Instance.ChangeState(GameState.MainMenu);
            }
        }

        private void OnQuitWithoutSavingClicked()
        {
            Time.timeScale = 1f;
            if (dialogRoot != null) dialogRoot.SetActive(false);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.Notify("Exited without saving.");
                GameManager.Instance.ChangeState(GameState.MainMenu);
            }
        }

        private void OnCancelClicked()
        {
            HideDialog();
        }

        private IEnumerator AnimateOpenRoutine()
        {
            float elapsed = 0f;
            float duration = 0.25f;

            if (cardContainer != null) cardContainer.localScale = Vector3.one * 0.75f;
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);

                if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t * 1.5f);
                if (cardContainer != null) cardContainer.localScale = Vector3.one * Mathf.Lerp(0.75f, 1f, easeOut);

                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (cardContainer != null) cardContainer.localScale = Vector3.one;
        }

        private IEnumerator AnimateCloseRoutine(Action onComplete)
        {
            float elapsed = 0f;
            float duration = 0.18f;

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

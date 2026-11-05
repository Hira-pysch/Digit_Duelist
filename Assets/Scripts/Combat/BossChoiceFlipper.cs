using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DigitDuelist
{
    /// <summary>
    /// Implements the Level 5 Boss Cognitive Disruption mechanic.
    /// Every 5th level (Level 5, 10, 15, etc.), dynamically flips and swaps the 4 multiple-choice
    /// answer buttons across the screen using smooth Coroutine/Lerp arced trajectories.
    /// </summary>
    public class BossChoiceFlipper : MonoBehaviour
    {
        public static BossChoiceFlipper Instance { get; private set; }

        [Header("Button References (4 Choices)")]
        [SerializeField] private RectTransform[] choiceButtonRects = new RectTransform[4];

        [Header("Disruption Timing")]
        [Tooltip("Seconds after a question begins before triggering a mid-question button swap.")]
        [SerializeField] private float midTurnSwapDelay = 2.8f;
        [Tooltip("Duration of the smooth slide & swap animation.")]
        [SerializeField] private float swapDuration = 0.42f;
        [Tooltip("Vertical arc displacement to simulate cards leaping over each other.")]
        [SerializeField] private float arcDisplacement = 28f;

        [Header("Warning Cue")]
        [SerializeField] private bool showPreSwapWarning = true;
        [SerializeField] private float warningDuration = 0.35f;

        // Default 2x2 grid home positions
        private Vector2[] homeGridPositions = new Vector2[4]
        {
            new Vector2(-160f, -110f), // Slot 0: Top-Left
            new Vector2(160f, -110f),  // Slot 1: Top-Right
            new Vector2(-160f, -190f), // Slot 2: Bottom-Left
            new Vector2(160f, -190f)   // Slot 3: Bottom-Right
        };

        // Tracks which button RectTransform is currently at which home slot
        private int[] currentSlotMapping = new int[4] { 0, 1, 2, 3 };

        private Coroutine activeSwapRoutine;
        private Coroutine activeDisruptionTimer;
        private bool isSwapping = false;

        public bool IsSwapping => isSwapping;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            AutoFindButtonRects();
        }

        private void Start()
        {
            AutoFindButtonRects();
            CacheHomePositions();
        }

        private void AutoFindButtonRects()
        {
            for (int i = 0; i < 4; i++)
            {
                if (choiceButtonRects[i] == null)
                {
                    GameObject btnGo = GameObject.Find($"Btn_Choice_{i}");
                    if (btnGo != null)
                    {
                        choiceButtonRects[i] = btnGo.GetComponent<RectTransform>();
                    }
                }
            }
        }

        private void CacheHomePositions()
        {
            for (int i = 0; i < 4; i++)
            {
                if (choiceButtonRects[i] != null)
                {
                    homeGridPositions[i] = choiceButtonRects[i].anchoredPosition;
                    currentSlotMapping[i] = i;
                }
            }
        }

        /// <summary>
        /// Called when a new question is delivered.
        /// If the current duel is a Boss fight (Lv. 5, 10, 15), arms the cognitive disruption.
        /// </summary>
        public void OnNewQuestionStarted(bool isBossLevel)
        {
            StopAllCoroutines();
            isSwapping = false;

            if (!isBossLevel)
            {
                ResetButtonsToDefaultHomePositions();
                return;
            }

            activeDisruptionTimer = StartCoroutine(BossDisruptionCycleRoutine());
        }

        private void Update()
        {
            // Debug test shortcut: Press 'B' during gameplay to immediately test button choice flipping
            if (GameInput.GetKeyDown(KeyCode.B))
            {
                TriggerTestSwap();
            }
        }

        public void TriggerTestSwap()
        {
            StopAllCoroutines();
            if (CombatFXController.Instance != null)
            {
                CombatFXController.Instance.SpawnFloatingText(Vector3.up * 1.5f, "👑 CHOICE FLIP ACTIVATED!", new Color(1f, 0.85f, 0.2f), 4f, true);
            }
            StartCoroutine(ExecuteSmoothSwapRoutine());
        }

        /// <summary>
        /// Manages the boss disruption cycle: initial jitter, mid-turn swap warning, and smooth position exchange.
        /// </summary>
        private IEnumerator BossDisruptionCycleRoutine()
        {
            if (CombatFXController.Instance != null)
            {
                CombatFXController.Instance.SpawnFloatingText(Vector3.up * 1.5f, "👑 BOSS BATTLE!\nCHOICES WILL SWAP!", new Color(1f, 0.85f, 0.2f), 4.5f, true);
            }

            // Loop swapping while question is active on boss levels
            while (true)
            {
                yield return new WaitForSeconds(midTurnSwapDelay);

                if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Gameplay)
                    yield break;

                // Trigger smooth button swap
                yield return StartCoroutine(ExecuteSmoothSwapRoutine());
            }
        }

        /// <summary>
        /// Smoothly swaps two or four buttons with an arced leap and scale-flip animation.
        /// </summary>
        public IEnumerator ExecuteSmoothSwapRoutine()
        {
            isSwapping = true;
            AutoFindButtonRects();

            // Pick swap pattern (0: Diagonal Swap 0<->3, 1: Horizontal Flip 0<->1 & 2<->3, 2: Clockwise 4-way shift)
            int pattern = UnityEngine.Random.Range(0, 3);
            int idxA = 0, idxB = 1;
            int idxC = -1, idxD = -1;

            if (pattern == 0) // Diagonal
            {
                idxA = 0; idxB = 3; // Top-Left <-> Bottom-Right
                idxC = 1; idxD = 2; // Top-Right <-> Bottom-Left
            }
            else if (pattern == 1) // Horizontal
            {
                idxA = 0; idxB = 1; // Top row
                idxC = 2; idxD = 3; // Bottom row
            }
            else // Vertical
            {
                idxA = 0; idxB = 2; // Left column
                idxC = 1; idxD = 3; // Right column
            }

            // 1. Subtle Warning Jitter / Anticipation Cue
            if (showPreSwapWarning)
            {
                yield return StartCoroutine(PlayWarningCueRoutine(idxA, idxB, idxC, idxD));
            }

            if (KidAudioManager.Instance != null)
            {
                KidAudioManager.Instance.PlayBubbleShield();
            }

            // 2. Smooth Lerp Swap with Arc Leap
            RectTransform rectA = choiceButtonRects[idxA];
            RectTransform rectB = choiceButtonRects[idxB];
            RectTransform rectC = (idxC >= 0) ? choiceButtonRects[idxC] : null;
            RectTransform rectD = (idxD >= 0) ? choiceButtonRects[idxD] : null;

            if (rectA == null || rectB == null)
            {
                isSwapping = false;
                yield break;
            }

            Vector2 startPosA = rectA.anchoredPosition;
            Vector2 startPosB = rectB.anchoredPosition;
            Vector2 startPosC = rectC != null ? rectC.anchoredPosition : Vector2.zero;
            Vector2 startPosD = rectD != null ? rectD.anchoredPosition : Vector2.zero;

            Vector2 targetPosA = startPosB;
            Vector2 targetPosB = startPosA;
            Vector2 targetPosC = startPosD;
            Vector2 targetPosD = startPosC;

            float elapsed = 0f;
            while (elapsed < swapDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / swapDuration);

                // Smooth Cosine Ease-In-Out
                float ease = 0.5f * (1f - Mathf.Cos(t * Mathf.PI));

                // Parabolic Arc: leaps upward/downward
                float arc = Mathf.Sin(t * Mathf.PI) * arcDisplacement;

                // Move Pair 1
                rectA.anchoredPosition = Vector2.Lerp(startPosA, targetPosA, ease) + new Vector2(0f, arc);
                rectB.anchoredPosition = Vector2.Lerp(startPosB, targetPosB, ease) - new Vector2(0f, arc);

                // Scale pulse on flip
                float scale = Mathf.Lerp(1f, 0.88f, Mathf.Sin(t * Mathf.PI));
                rectA.localScale = Vector3.one * scale;
                rectB.localScale = Vector3.one * scale;

                // Move Pair 2 if present
                if (rectC != null && rectD != null)
                {
                    rectC.anchoredPosition = Vector2.Lerp(startPosC, targetPosC, ease) + new Vector2(0f, -arc * 0.7f);
                    rectD.anchoredPosition = Vector2.Lerp(startPosD, targetPosD, ease) + new Vector2(0f, arc * 0.7f);
                    rectC.localScale = Vector3.one * scale;
                    rectD.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            // Snap cleanly to target coordinates
            rectA.anchoredPosition = targetPosA;
            rectB.anchoredPosition = targetPosB;
            rectA.localScale = Vector3.one;
            rectB.localScale = Vector3.one;

            if (rectC != null && rectD != null)
            {
                rectC.anchoredPosition = targetPosC;
                rectD.anchoredPosition = targetPosD;
                rectC.localScale = Vector3.one;
                rectD.localScale = Vector3.one;
            }

            isSwapping = false;
        }

        private IEnumerator PlayWarningCueRoutine(int a, int b, int c, int d)
        {
            float elapsed = 0f;
            while (elapsed < warningDuration)
            {
                elapsed += Time.deltaTime;
                float shake = Mathf.Sin(elapsed * 45f) * 3.5f;

                if (choiceButtonRects[a] != null) choiceButtonRects[a].localRotation = Quaternion.Euler(0f, 0f, shake);
                if (choiceButtonRects[b] != null) choiceButtonRects[b].localRotation = Quaternion.Euler(0f, 0f, -shake);
                if (c >= 0 && choiceButtonRects[c] != null) choiceButtonRects[c].localRotation = Quaternion.Euler(0f, 0f, -shake);
                if (d >= 0 && choiceButtonRects[d] != null) choiceButtonRects[d].localRotation = Quaternion.Euler(0f, 0f, shake);

                yield return null;
            }

            for (int i = 0; i < 4; i++)
            {
                if (choiceButtonRects[i] != null) choiceButtonRects[i].localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Restores all buttons back to their default non-boss 2x2 grid home positions.
        /// </summary>
        public void ResetButtonsToDefaultHomePositions()
        {
            StopAllCoroutines();
            isSwapping = false;

            for (int i = 0; i < 4; i++)
            {
                if (choiceButtonRects[i] != null)
                {
                    choiceButtonRects[i].anchoredPosition = homeGridPositions[i];
                    choiceButtonRects[i].localScale = Vector3.one;
                    choiceButtonRects[i].localRotation = Quaternion.identity;
                }
            }
        }
    }
}

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

        [Header("Disruption Settings")]
        [Tooltip("When true, choice buttons swap positions during boss battles.")]
        [SerializeField] private bool enableButtonSwapping = true;

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

        // Immutable standard 2x2 grid slot coordinates
        public static readonly Vector2[] SlotPositions = new Vector2[4]
        {
            new Vector2(-160f, -110f), // Slot 0: Top-Left
            new Vector2(160f, -110f),  // Slot 1: Top-Right
            new Vector2(-160f, -190f), // Slot 2: Bottom-Left
            new Vector2(160f, -190f)   // Slot 3: Bottom-Right
        };

        // buttonAtSlot[s] tracks which choice button GameObject index (0..3) currently occupies slot s.
        // This guarantees a strict 1-to-1 bijection so buttons can NEVER overlap or combine.
        private int[] buttonAtSlot = new int[4] { 0, 1, 2, 3 };

        // Active swap tracking to safely resolve mid-flight interruptions
        private int activeSwapSlotA = -1, activeSwapSlotB = -1, activeSwapSlotC = -1, activeSwapSlotD = -1;
        private int activeSwapBtnA = -1, activeSwapBtnB = -1, activeSwapBtnC = -1, activeSwapBtnD = -1;

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
            ResetButtonsToDefaultHomePositions();
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

        /// <summary>
        /// Called when a new question is delivered.
        /// If button swapping is enabled and the duel is a Boss fight, arms the cognitive disruption.
        /// </summary>
        public void OnNewQuestionStarted(bool isBossLevel)
        {
            StopAllRunningCoroutines();

            // Always ensure buttons begin each question cleanly in their standard slots
            ResetButtonsToDefaultHomePositions();

            if (!enableButtonSwapping || !isBossLevel)
            {
                return;
            }

            activeDisruptionTimer = StartCoroutine(BossDisruptionCycleRoutine());
        }

        /// <summary>
        /// Called when the player submits an answer, turns timeout, or the turn ends.
        /// Immediately completes any mid-flight animation and snaps buttons to valid slots.
        /// </summary>
        public void OnTurnEnded()
        {
            StopAllRunningCoroutines();
            SnapAllButtonsToSlots();
        }

        private void StopAllRunningCoroutines()
        {
            if (activeDisruptionTimer != null)
            {
                StopCoroutine(activeDisruptionTimer);
                activeDisruptionTimer = null;
            }

            if (activeSwapRoutine != null)
            {
                StopCoroutine(activeSwapRoutine);
                activeSwapRoutine = null;
            }

            if (isSwapping)
            {
                CompleteActiveSwapImmediately();
                isSwapping = false;
            }

            StopAllCoroutines();
        }

        private void Update()
        {
            // Debug test shortcut: Press 'B' during gameplay to immediately test button choice flipping
            if (enableButtonSwapping && GameInput.GetKeyDown(KeyCode.B))
            {
                TriggerTestSwap();
            }
        }

        public void TriggerTestSwap()
        {
            StopAllRunningCoroutines();
            if (CombatFXController.Instance != null)
            {
                CombatFXController.Instance.SpawnFloatingText(Vector3.up * 1.5f, "👑 CHOICE FLIP ACTIVATED!", new Color(1f, 0.85f, 0.2f), 4f, true);
            }
            activeSwapRoutine = StartCoroutine(ExecuteSmoothSwapRoutine());
        }

        /// <summary>
        /// Manages the boss disruption cycle: mid-turn swap warning and smooth position exchange.
        /// </summary>
        private IEnumerator BossDisruptionCycleRoutine()
        {
            if (CombatFXController.Instance != null)
            {
                CombatFXController.Instance.SpawnFloatingText(Vector3.up * 1.5f, "👑 BOSS BATTLE!\nCHOICES WILL SWAP!", new Color(1f, 0.85f, 0.2f), 4.5f, true);
            }

            while (true)
            {
                yield return new WaitForSeconds(midTurnSwapDelay);

                if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Gameplay)
                    yield break;

                activeSwapRoutine = StartCoroutine(ExecuteSmoothSwapRoutine());
                yield return activeSwapRoutine;
                activeSwapRoutine = null;
            }
        }

        /// <summary>
        /// Smoothly swaps button positions using slot-based mapping.
        /// Targets slots rather than raw indices to ensure buttons NEVER share a coordinate.
        /// </summary>
        public IEnumerator ExecuteSmoothSwapRoutine()
        {
            isSwapping = true;
            AutoFindButtonRects();

            // Pick swap pattern across the 4 SLOTS:
            // 0: Horizontal (Slot 0 <-> Slot 1, Slot 2 <-> Slot 3)
            // 1: Vertical (Slot 0 <-> Slot 2, Slot 1 <-> Slot 3)
            // 2: Diagonal (Slot 0 <-> Slot 3, Slot 1 <-> Slot 2)
            int pattern = UnityEngine.Random.Range(0, 3);
            int slotA = 0, slotB = 1;
            int slotC = 2, slotD = 3;

            if (pattern == 0) // Horizontal
            {
                slotA = 0; slotB = 1;
                slotC = 2; slotD = 3;
            }
            else if (pattern == 1) // Vertical
            {
                slotA = 0; slotB = 2;
                slotC = 1; slotD = 3;
            }
            else // Diagonal
            {
                slotA = 0; slotB = 3;
                slotC = 1; slotD = 2;
            }

            int btnIdxA = buttonAtSlot[slotA];
            int btnIdxB = buttonAtSlot[slotB];
            int btnIdxC = buttonAtSlot[slotC];
            int btnIdxD = buttonAtSlot[slotD];

            // Store active swap state in case of early interruption
            activeSwapSlotA = slotA; activeSwapSlotB = slotB;
            activeSwapSlotC = slotC; activeSwapSlotD = slotD;
            activeSwapBtnA = btnIdxA; activeSwapBtnB = btnIdxB;
            activeSwapBtnC = btnIdxC; activeSwapBtnD = btnIdxD;

            // 1. Subtle Warning Jitter / Anticipation Cue
            if (showPreSwapWarning)
            {
                yield return StartCoroutine(PlayWarningCueRoutine(btnIdxA, btnIdxB, btnIdxC, btnIdxD));
            }

            if (KidAudioManager.Instance != null)
            {
                KidAudioManager.Instance.PlayBubbleShield();
            }

            // 2. Smooth Lerp Swap with Arc Leap
            RectTransform rectA = choiceButtonRects[btnIdxA];
            RectTransform rectB = choiceButtonRects[btnIdxB];
            RectTransform rectC = choiceButtonRects[btnIdxC];
            RectTransform rectD = choiceButtonRects[btnIdxD];

            if (rectA == null || rectB == null || rectC == null || rectD == null)
            {
                isSwapping = false;
                yield break;
            }

            // Source and destination are strictly defined by SlotPositions
            Vector2 startPosA = SlotPositions[slotA];
            Vector2 targetPosA = SlotPositions[slotB];

            Vector2 startPosB = SlotPositions[slotB];
            Vector2 targetPosB = SlotPositions[slotA];

            Vector2 startPosC = SlotPositions[slotC];
            Vector2 targetPosC = SlotPositions[slotD];

            Vector2 startPosD = SlotPositions[slotD];
            Vector2 targetPosD = SlotPositions[slotC];

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

                // Move Pair 2
                rectC.anchoredPosition = Vector2.Lerp(startPosC, targetPosC, ease) + new Vector2(0f, -arc * 0.7f);
                rectD.anchoredPosition = Vector2.Lerp(startPosD, targetPosD, ease) + new Vector2(0f, arc * 0.7f);

                // Scale pulse on flip
                float scale = Mathf.Lerp(1f, 0.90f, Mathf.Sin(t * Mathf.PI));
                rectA.localScale = Vector3.one * scale;
                rectB.localScale = Vector3.one * scale;
                rectC.localScale = Vector3.one * scale;
                rectD.localScale = Vector3.one * scale;

                yield return null;
            }

            // Snap cleanly to target coordinates
            rectA.anchoredPosition = targetPosA;
            rectB.anchoredPosition = targetPosB;
            rectC.anchoredPosition = targetPosC;
            rectD.anchoredPosition = targetPosD;

            rectA.localScale = Vector3.one;
            rectB.localScale = Vector3.one;
            rectC.localScale = Vector3.one;
            rectD.localScale = Vector3.one;

            rectA.localRotation = Quaternion.identity;
            rectB.localRotation = Quaternion.identity;
            rectC.localRotation = Quaternion.identity;
            rectD.localRotation = Quaternion.identity;

            // Update slot tracking strictly
            buttonAtSlot[slotA] = btnIdxB;
            buttonAtSlot[slotB] = btnIdxA;
            buttonAtSlot[slotC] = btnIdxD;
            buttonAtSlot[slotD] = btnIdxC;

            activeSwapSlotA = activeSwapSlotB = activeSwapSlotC = activeSwapSlotD = -1;
            activeSwapBtnA = activeSwapBtnB = activeSwapBtnC = activeSwapBtnD = -1;

            isSwapping = false;
        }

        private void CompleteActiveSwapImmediately()
        {
            if (activeSwapSlotA >= 0 && activeSwapSlotB >= 0 && activeSwapSlotC >= 0 && activeSwapSlotD >= 0)
            {
                if (activeSwapBtnA >= 0 && choiceButtonRects[activeSwapBtnA] != null)
                {
                    choiceButtonRects[activeSwapBtnA].anchoredPosition = SlotPositions[activeSwapSlotB];
                    choiceButtonRects[activeSwapBtnA].localScale = Vector3.one;
                    choiceButtonRects[activeSwapBtnA].localRotation = Quaternion.identity;
                }
                if (activeSwapBtnB >= 0 && choiceButtonRects[activeSwapBtnB] != null)
                {
                    choiceButtonRects[activeSwapBtnB].anchoredPosition = SlotPositions[activeSwapSlotA];
                    choiceButtonRects[activeSwapBtnB].localScale = Vector3.one;
                    choiceButtonRects[activeSwapBtnB].localRotation = Quaternion.identity;
                }
                if (activeSwapBtnC >= 0 && choiceButtonRects[activeSwapBtnC] != null)
                {
                    choiceButtonRects[activeSwapBtnC].anchoredPosition = SlotPositions[activeSwapSlotD];
                    choiceButtonRects[activeSwapBtnC].localScale = Vector3.one;
                    choiceButtonRects[activeSwapBtnC].localRotation = Quaternion.identity;
                }
                if (activeSwapBtnD >= 0 && choiceButtonRects[activeSwapBtnD] != null)
                {
                    choiceButtonRects[activeSwapBtnD].anchoredPosition = SlotPositions[activeSwapSlotC];
                    choiceButtonRects[activeSwapBtnD].localScale = Vector3.one;
                    choiceButtonRects[activeSwapBtnD].localRotation = Quaternion.identity;
                }

                buttonAtSlot[activeSwapSlotA] = activeSwapBtnB;
                buttonAtSlot[activeSwapSlotB] = activeSwapBtnA;
                buttonAtSlot[activeSwapSlotC] = activeSwapBtnD;
                buttonAtSlot[activeSwapSlotD] = activeSwapBtnC;

                activeSwapSlotA = activeSwapSlotB = activeSwapSlotC = activeSwapSlotD = -1;
                activeSwapBtnA = activeSwapBtnB = activeSwapBtnC = activeSwapBtnD = -1;
            }
        }

        private IEnumerator PlayWarningCueRoutine(int btnA, int btnB, int btnC, int btnD)
        {
            float elapsed = 0f;
            while (elapsed < warningDuration)
            {
                elapsed += Time.deltaTime;
                float shake = Mathf.Sin(elapsed * 45f) * 3.5f;

                if (choiceButtonRects[btnA] != null) choiceButtonRects[btnA].localRotation = Quaternion.Euler(0f, 0f, shake);
                if (choiceButtonRects[btnB] != null) choiceButtonRects[btnB].localRotation = Quaternion.Euler(0f, 0f, -shake);
                if (choiceButtonRects[btnC] != null) choiceButtonRects[btnC].localRotation = Quaternion.Euler(0f, 0f, -shake);
                if (choiceButtonRects[btnD] != null) choiceButtonRects[btnD].localRotation = Quaternion.Euler(0f, 0f, shake);

                yield return null;
            }

            for (int i = 0; i < 4; i++)
            {
                if (choiceButtonRects[i] != null) choiceButtonRects[i].localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Snaps every button into its currently tracked slot coordinate.
        /// </summary>
        public void SnapAllButtonsToSlots()
        {
            AutoFindButtonRects();
            for (int s = 0; s < 4; s++)
            {
                int btnIdx = buttonAtSlot[s];
                if (btnIdx >= 0 && btnIdx < 4 && choiceButtonRects[btnIdx] != null)
                {
                    choiceButtonRects[btnIdx].anchoredPosition = SlotPositions[s];
                    choiceButtonRects[btnIdx].localScale = Vector3.one;
                    choiceButtonRects[btnIdx].localRotation = Quaternion.identity;
                }
            }
        }

        /// <summary>
        /// Restores all buttons back to their default fixed 2x2 grid home positions.
        /// </summary>
        public void ResetButtonsToDefaultHomePositions()
        {
            StopAllRunningCoroutines();
            AutoFindButtonRects();

            for (int i = 0; i < 4; i++)
            {
                buttonAtSlot[i] = i;
                if (choiceButtonRects[i] != null)
                {
                    choiceButtonRects[i].anchoredPosition = SlotPositions[i];
                    choiceButtonRects[i].localScale = Vector3.one;
                    choiceButtonRects[i].localRotation = Quaternion.identity;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DigitDuelist
{
    public enum AssistSpellType
    {
        FreezeTime,
        CrystalBall,
        BubbleShield
    }

    /// <summary>
    /// Controls kid-friendly consumable assist abilities:
    /// 1. Freeze Time: Grants 10 seconds of breathing room or freezes timer.
    /// 2. Crystal Ball (50/50): Eliminates two wrong choices with a sparkle.
    /// 3. Bubble Shield: Absorbs the next arithmetic mistake without losing health.
    /// </summary>
    public class AssistSpellsController : MonoBehaviour
    {
        public static AssistSpellsController Instance { get; private set; }

        [Header("Dependencies")]
        [SerializeField] private DuelController duelController;
        [SerializeField] private UIManager uiManager;

        [Header("Spell Charges Per Duel")]
        [SerializeField] private int maxFreezeCharges = 2;
        [SerializeField] private int maxCrystalBallCharges = 2;
        [SerializeField] private int maxShieldCharges = 2;

        private int currentFreezeCharges;
        private int currentCrystalBallCharges;
        private int currentShieldCharges;

        [Header("Runtime State")]
        private bool isTimerFrozen = false;
        private float freezeTimeRemaining = 0f;
        private bool hasActiveBubbleShield = false;
        private GameObject activeBubbleShieldVfx;

        // Events
        public event Action<AssistSpellType, int> OnSpellChargesChanged;
        public event Action<bool> OnBubbleShieldStateChanged;
        public event Action<float> OnTimerFrozen;

        public int FreezeCharges => currentFreezeCharges;
        public int CrystalBallCharges => currentCrystalBallCharges;
        public int ShieldCharges => currentShieldCharges;
        public bool HasActiveBubbleShield => hasActiveBubbleShield;
        public bool IsTimerFrozen => isTimerFrozen;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);

            if (duelController == null) duelController = FindFirstObjectByType<DuelController>();
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();

            ResetCharges();
        }

        private void Update()
        {
            if (isTimerFrozen)
            {
                freezeTimeRemaining -= Time.deltaTime;
                if (freezeTimeRemaining <= 0f)
                {
                    isTimerFrozen = false;
                }
            }
        }

        public void ResetCharges()
        {
            currentFreezeCharges = maxFreezeCharges;
            currentCrystalBallCharges = maxCrystalBallCharges;
            currentShieldCharges = maxShieldCharges;
            hasActiveBubbleShield = false;
            isTimerFrozen = false;

            DestroyBubbleVfx();

            NotifyCharges();
        }

        private void NotifyCharges()
        {
            OnSpellChargesChanged?.Invoke(AssistSpellType.FreezeTime, currentFreezeCharges);
            OnSpellChargesChanged?.Invoke(AssistSpellType.CrystalBall, currentCrystalBallCharges);
            OnSpellChargesChanged?.Invoke(AssistSpellType.BubbleShield, currentShieldCharges);
            OnBubbleShieldStateChanged?.Invoke(hasActiveBubbleShield);
        }

        #region Spell 1: Freeze Time

        public bool CanUseFreeze() => currentFreezeCharges > 0 && !isTimerFrozen;

        public void CastFreezeTime(float duration = 10f)
        {
            if (!CanUseFreeze()) return;

            currentFreezeCharges--;
            isTimerFrozen = true;
            freezeTimeRemaining = duration;

            if (KidAudioManager.Instance != null) KidAudioManager.Instance.PlayFreezeSpell();

            if (CombatFXController.Instance != null && duelController != null && duelController.PlayerVisuals != null)
            {
                CombatFXController.Instance.SpawnFloatingText(
                    duelController.PlayerVisuals.transform.position + Vector3.up * 1.5f,
                    "❄️ TIME FROZEN!",
                    new Color(0.4f, 0.85f, 1f),
                    4.5f
                );
            }

            OnTimerFrozen?.Invoke(duration);
            OnSpellChargesChanged?.Invoke(AssistSpellType.FreezeTime, currentFreezeCharges);
        }

        #endregion

        #region Spell 2: Crystal Ball (50/50)

        public bool CanUseCrystalBall() => currentCrystalBallCharges > 0 && duelController != null && duelController.CurrentQuestion.choices != null;

        public void CastCrystalBall()
        {
            if (!CanUseCrystalBall()) return;

            MathQuestion q = duelController.CurrentQuestion;
            if (q.choices == null || q.choices.Length < 4) return;

            currentCrystalBallCharges--;

            // Find all wrong answer indices
            List<int> wrongIndices = new List<int>();
            for (int i = 0; i < q.choices.Length; i++)
            {
                if (q.choices[i] != q.correctAnswer)
                {
                    wrongIndices.Add(i);
                }
            }

            // Shuffle and pick 2 wrong answers to eliminate
            for (int i = wrongIndices.Count - 1; i > 0; i--)
            {
                int r = UnityEngine.Random.Range(0, i + 1);
                int temp = wrongIndices[i];
                wrongIndices[i] = wrongIndices[r];
                wrongIndices[r] = temp;
            }

            List<int> eliminatedIndices = new List<int> { wrongIndices[0], wrongIndices[1] };

            // Disable buttons in UI
            foreach (int idx in eliminatedIndices)
            {
                GameObject btnGo = GameObject.Find($"Btn_Choice_{idx}");
                if (btnGo != null)
                {
                    Button b = btnGo.GetComponent<Button>();
                    if (b != null) b.interactable = false;

                    Image img = btnGo.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.35f, 0.35f, 0.35f, 0.4f);

                    var tmp = btnGo.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (tmp != null) tmp.text = "✖";
                }
            }

            if (KidAudioManager.Instance != null) KidAudioManager.Instance.PlayCorrectAnswer();

            if (CombatFXController.Instance != null)
            {
                CombatFXController.Instance.SpawnFloatingText(
                    Vector3.zero,
                    "🔮 50/50 REVEAL!",
                    new Color(0.85f, 0.5f, 1f),
                    4.5f
                );
            }

            OnSpellChargesChanged?.Invoke(AssistSpellType.CrystalBall, currentCrystalBallCharges);
        }

        #endregion

        #region Spell 3: Bubble Shield

        public bool CanUseBubbleShield() => currentShieldCharges > 0 && !hasActiveBubbleShield;

        public void CastBubbleShield()
        {
            if (!CanUseBubbleShield()) return;

            currentShieldCharges--;
            hasActiveBubbleShield = true;

            SpawnBubbleVfx();

            if (KidAudioManager.Instance != null) KidAudioManager.Instance.PlayBubbleShield();

            if (CombatFXController.Instance != null && duelController != null && duelController.PlayerVisuals != null)
            {
                CombatFXController.Instance.SpawnFloatingText(
                    duelController.PlayerVisuals.transform.position + Vector3.up * 1.5f,
                    "🛡️ BUBBLE SHIELD ACTIVE!",
                    new Color(0.3f, 1f, 0.6f),
                    4.5f
                );
            }

            OnBubbleShieldStateChanged?.Invoke(true);
            OnSpellChargesChanged?.Invoke(AssistSpellType.BubbleShield, currentShieldCharges);
        }

        /// <summary>
        /// Checks if player is protected by the bubble shield.
        /// If active, absorbs the damage, pops the bubble, and returns TRUE.
        /// </summary>
        public bool TryAbsorbDamageWithShield()
        {
            if (!hasActiveBubbleShield) return false;

            hasActiveBubbleShield = false;
            DestroyBubbleVfx();

            if (KidAudioManager.Instance != null) KidAudioManager.Instance.PlayBubbleShield();

            if (CombatFXController.Instance != null && duelController != null && duelController.PlayerVisuals != null)
            {
                CombatFXController.Instance.SpawnFloatingText(
                    duelController.PlayerVisuals.transform.position + Vector3.up * 1.6f,
                    "✨ SHIELD ABSORBED MISTAKE! ✨",
                    new Color(0.4f, 1f, 0.8f),
                    5f,
                    true
                );
            }

            OnBubbleShieldStateChanged?.Invoke(false);
            return true;
        }

        private void SpawnBubbleVfx()
        {
            DestroyBubbleVfx();

            if (duelController != null && duelController.PlayerVisuals != null)
            {
                activeBubbleShieldVfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                activeBubbleShieldVfx.name = "Player_BubbleShield";
                Destroy(activeBubbleShieldVfx.GetComponent<Collider>());

                Transform pTrans = duelController.PlayerVisuals.transform;
                activeBubbleShieldVfx.transform.SetParent(pTrans);
                activeBubbleShieldVfx.transform.localPosition = new Vector3(0f, 1.1f, 0f);
                activeBubbleShieldVfx.transform.localScale = new Vector3(2.2f, 2.4f, 2.2f);

                Renderer r = activeBubbleShieldVfx.GetComponent<Renderer>();
                if (r != null)
                {
                    Material bubbleMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
                    bubbleMat.color = new Color(0.3f, 0.9f, 1f, 0.35f);
                    r.material = bubbleMat;
                }
            }
        }

        private void DestroyBubbleVfx()
        {
            if (activeBubbleShieldVfx != null)
            {
                Destroy(activeBubbleShieldVfx);
                activeBubbleShieldVfx = null;
            }
        }

        #endregion
    }
}

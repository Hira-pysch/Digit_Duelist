using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DigitDuelist
{
    /// <summary>
    /// Manages procedural combat choreography, idle breathing, attack lunges,
    /// weapon slashes, damage recoil, hit flashes, victory triumph, and defeat collapse.
    /// </summary>
    public class CharacterDuelVisuals : MonoBehaviour
    {
        [Header("Character Identity")]
        [SerializeField] private bool isPlayer = true;
        [SerializeField] private Transform weaponRightHand;
        [SerializeField] private Transform shieldLeftHand;
        [SerializeField] private Animator animator;

        [Header("Procedural Idle")]
        [SerializeField] private bool enableIdleBreathing = true;
        [SerializeField] private float breatheSpeed = 2.4f;
        [SerializeField] private float breatheHeight = 0.06f;
        [SerializeField] private float idleSwayAngle = 2.5f;

        [Header("Combat Movement Settings")]
        [SerializeField] private float lungeDistance = 2.2f;
        [SerializeField] private float attackDuration = 0.45f;
        [SerializeField] private float recoilDistance = 0.6f;

        private Vector3 defaultPosition;
        private Quaternion defaultRotation;
        private Vector3 weaponDefaultLocalPos;
        private Quaternion weaponDefaultLocalRot;
        private TrailRenderer weaponTrail;

        private Renderer[] characterRenderers;
        private List<Color> originalColors = new List<Color>();
        private bool isPerformingAction = false;
        private bool isDead = false;

        public bool IsPlayer => isPlayer;
        public Vector3 DefaultPosition => defaultPosition;
        public Animator CharacterAnimator => animator;

        private void Awake()
        {
            defaultPosition = transform.localPosition;
            defaultRotation = transform.localRotation;

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (weaponRightHand != null)
            {
                weaponDefaultLocalPos = weaponRightHand.localPosition;
                weaponDefaultLocalRot = weaponRightHand.localRotation;
                SetupWeaponTrail();
            }

            CacheRenderers();
        }

        private void SetupWeaponTrail()
        {
            if (weaponRightHand == null) return;
            weaponTrail = weaponRightHand.GetComponentInChildren<TrailRenderer>();
            if (weaponTrail == null)
            {
                GameObject trailGo = new GameObject("WeaponTrailVFX");
                trailGo.transform.SetParent(weaponRightHand, false);
                trailGo.transform.localPosition = new Vector3(0f, 0.7f, 0f);

                weaponTrail = trailGo.AddComponent<TrailRenderer>();
                weaponTrail.time = 0.22f;
                weaponTrail.startWidth = 0.24f;
                weaponTrail.endWidth = 0.01f;
                weaponTrail.minVertexDistance = 0.04f;

                Material trailMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Sprites/Default"));
                Color trailCol = isPlayer ? new Color(0.2f, 0.88f, 1f, 0.9f) : new Color(1f, 0.45f, 0.15f, 0.9f);
                if (trailMat.HasProperty("_BaseColor")) trailMat.SetColor("_BaseColor", trailCol);
                else if (trailMat.HasProperty("_Color")) trailMat.color = trailCol;

                weaponTrail.material = trailMat;
                weaponTrail.startColor = trailCol;
                weaponTrail.endColor = new Color(trailCol.r, trailCol.g, trailCol.b, 0f);
                weaponTrail.emitting = false;
            }
        }

        private void Start()
        {
            defaultPosition = transform.localPosition;
            defaultRotation = transform.localRotation;
        }

        private void CacheRenderers()
        {
            characterRenderers = GetComponentsInChildren<Renderer>();
            originalColors.Clear();
            foreach (var r in characterRenderers)
            {
                if (r.material != null && r.material.HasProperty("_BaseColor"))
                {
                    originalColors.Add(r.material.GetColor("_BaseColor"));
                }
                else if (r.material != null && r.material.HasProperty("_Color"))
                {
                    originalColors.Add(r.material.color);
                }
                else
                {
                    originalColors.Add(Color.white);
                }
            }
        }

        private void Update()
        {
            if (isDead || isPerformingAction || !enableIdleBreathing) return;

            // Natural combat breathing and stance sway
            float offset = (isPlayer ? 0f : 1.5f);
            float time = (Time.time * breatheSpeed) + offset;
            float verticalOffset = Mathf.Sin(time) * breatheHeight;
            float sway = Mathf.Sin(time * 0.8f) * idleSwayAngle;

            transform.localPosition = defaultPosition + new Vector3(0f, verticalOffset, 0f);
            transform.localRotation = defaultRotation * Quaternion.Euler(0f, 0f, sway);

            if (weaponRightHand != null)
            {
                float weaponBob = Mathf.Sin(time * 1.2f) * 3f;
                weaponRightHand.localRotation = weaponDefaultLocalRot * Quaternion.Euler(weaponBob, 0f, 0f);
            }
        }

        /// <summary>
        /// Executes forward attack lunge with sword slash against opponent.
        /// </summary>
        public void PlayAttack(Vector3 targetDirection, Action onImpactCallback)
        {
            if (isDead) return;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("Attack");
            }
            StartCoroutine(AttackRoutine(targetDirection.normalized, onImpactCallback));
        }

        private IEnumerator AttackRoutine(Vector3 direction, Action onImpactCallback)
        {
            isPerformingAction = true;
            Vector3 startPos = defaultPosition;
            Vector3 lungePos = startPos + (direction * lungeDistance);

            // 1. Wind-up step back
            float windupTime = attackDuration * 0.25f;
            float elapsed = 0f;
            while (elapsed < windupTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / windupTime;
                transform.localPosition = Vector3.Lerp(startPos, startPos - (direction * 0.3f), t);
                if (weaponRightHand != null)
                {
                    weaponRightHand.localRotation = weaponDefaultLocalRot * Quaternion.Euler(-35f * t, 0f, 20f * t);
                }
                yield return null;
            }

            // Enable weapon trail arc
            if (weaponTrail != null)
            {
                weaponTrail.Clear();
                weaponTrail.emitting = true;
            }

            // 2. Swift Forward Lunge & Strike
            float strikeTime = attackDuration * 0.35f;
            elapsed = 0f;
            while (elapsed < strikeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / strikeTime;
                float easeIn = t * t;
                transform.localPosition = Vector3.Lerp(startPos - (direction * 0.3f), lungePos, easeIn);

                if (weaponRightHand != null)
                {
                    // Dramatic slash rotation
                    weaponRightHand.localRotation = weaponDefaultLocalRot * Quaternion.Euler(Mathf.Lerp(-35f, 65f, easeIn), 0f, -25f * easeIn);
                }
                yield return null;
            }

            // Impact trigger
            onImpactCallback?.Invoke();

            // Disable trail after impact
            if (weaponTrail != null)
            {
                weaponTrail.emitting = false;
            }

            // 3. Brief Impact pause
            yield return new WaitForSeconds(0.08f);

            // 4. Return to stance
            float returnTime = attackDuration * 0.4f;
            elapsed = 0f;
            while (elapsed < returnTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / returnTime;
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);
                transform.localPosition = Vector3.Lerp(lungePos, startPos, easeOut);

                if (weaponRightHand != null)
                {
                    weaponRightHand.localRotation = Quaternion.Slerp(weaponRightHand.localRotation, weaponDefaultLocalRot, easeOut);
                }
                yield return null;
            }

            transform.localPosition = defaultPosition;
            transform.localRotation = defaultRotation;
            if (weaponRightHand != null)
            {
                weaponRightHand.localPosition = weaponDefaultLocalPos;
                weaponRightHand.localRotation = weaponDefaultLocalRot;
            }

            isPerformingAction = false;
        }

        /// <summary>
        /// Plays hit recoil, red damage flash, and character shake upon receiving damage.
        /// </summary>
        public void PlayHitReaction(Vector3 hitFromDirection)
        {
            if (isDead) return;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("Hit");
            }
            StartCoroutine(HitReactionRoutine(hitFromDirection.normalized));
        }

        private IEnumerator HitReactionRoutine(Vector3 hitDirection)
        {
            isPerformingAction = true;
            Vector3 startPos = defaultPosition;
            Vector3 recoilPos = startPos - (hitDirection * recoilDistance);

            // Flash character red/white
            SetHitFlash(true);

            // Recoil back
            float recoilTime = 0.12f;
            float elapsed = 0f;
            while (elapsed < recoilTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / recoilTime;
                transform.localPosition = Vector3.Lerp(startPos, recoilPos, t);
                transform.localRotation = defaultRotation * Quaternion.Euler(0f, 0f, isPlayer ? -15f * t : 15f * t);
                yield return null;
            }

            SetHitFlash(false);

            // Shake & recover
            float recoverTime = 0.25f;
            elapsed = 0f;
            while (elapsed < recoverTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / recoverTime;
                float shake = Mathf.Sin(elapsed * 40f) * (1f - t) * 0.1f;
                transform.localPosition = Vector3.Lerp(recoilPos, startPos, t) + new Vector3(0f, 0f, shake);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, defaultRotation, t);
                yield return null;
            }

            transform.localPosition = defaultPosition;
            transform.localRotation = defaultRotation;
            isPerformingAction = false;
        }

        private void SetHitFlash(bool flash)
        {
            if (characterRenderers == null) return;

            for (int i = 0; i < characterRenderers.Length; i++)
            {
                var r = characterRenderers[i];
                if (r != null && r.material != null)
                {
                    Color targetCol = flash ? new Color(1f, 0.3f, 0.3f, 1f) : (i < originalColors.Count ? originalColors[i] : Color.white);
                    if (r.material.HasProperty("_BaseColor"))
                        r.material.SetColor("_BaseColor", targetCol);
                    else if (r.material.HasProperty("_Color"))
                        r.material.color = targetCol;
                }
            }
        }

        /// <summary>
        /// Plays triumphant victory pose (sword raised high).
        /// </summary>
        public void PlayVictory()
        {
            if (isDead) return;
            StopAllCoroutines();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("Victory");
            }
            StartCoroutine(VictoryRoutine());
        }

        private IEnumerator VictoryRoutine()
        {
            isPerformingAction = true;
            float elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.4f;
                if (weaponRightHand != null)
                {
                    weaponRightHand.localRotation = weaponDefaultLocalRot * Quaternion.Euler(-90f * t, 0f, 45f * t);
                }
                yield return null;
            }

            // Cheerful jump bobbing
            float timer = 0f;
            while (true)
            {
                timer += Time.deltaTime * 6f;
                float jump = Mathf.Abs(Mathf.Sin(timer)) * 0.25f;
                transform.localPosition = defaultPosition + new Vector3(0f, jump, 0f);
                yield return null;
            }
        }

        /// <summary>
        /// Plays defeat collapse when health is depleted.
        /// </summary>
        public void PlayDefeat()
        {
            StopAllCoroutines();
            isDead = true;
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetTrigger("Defeat");
            }
            StartCoroutine(DefeatRoutine());
        }

        private IEnumerator DefeatRoutine()
        {
            float elapsed = 0f;
            float duration = 0.5f;
            Quaternion fallRot = defaultRotation * Quaternion.Euler(isPlayer ? -80f : 80f, 0f, isPlayer ? 30f : -30f);
            Vector3 groundPos = defaultPosition + new Vector3(0f, -0.4f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeIn = t * t;

                transform.localPosition = Vector3.Lerp(defaultPosition, groundPos, easeIn);
                transform.localRotation = Quaternion.Slerp(defaultRotation, fallRot, easeIn);
                yield return null;
            }

            transform.localPosition = groundPos;
            transform.localRotation = fallRot;
        }

        /// <summary>
        /// Resets character stance and state for a new duel.
        /// </summary>
        public void ResetStance()
        {
            StopAllCoroutines();
            isDead = false;
            isPerformingAction = false;
            SetHitFlash(false);

            if (weaponTrail != null)
            {
                weaponTrail.emitting = false;
                weaponTrail.Clear();
            }

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }

            transform.localPosition = defaultPosition;
            transform.localRotation = defaultRotation;

            if (weaponRightHand != null)
            {
                weaponRightHand.localPosition = weaponDefaultLocalPos;
                weaponRightHand.localRotation = weaponDefaultLocalRot;
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

namespace DigitDuelist
{
    /// <summary>
    /// Spawns dynamic floating combat text, manages camera impact shake,
    /// and triggers combat hit particle / flash effects.
    /// </summary>
    public class CombatFXController : MonoBehaviour
    {
        public static CombatFXController Instance { get; private set; }

        [Header("Camera Shake & Action Cam")]
        [SerializeField] private Camera targetCamera;
        private Vector3 originalCamPos;
        private float originalFOV = 48f;
        private Coroutine shakeCoroutine;
        private Coroutine zoomCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(this);

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                originalCamPos = targetCamera.transform.localPosition;
                originalFOV = targetCamera.fieldOfView;
            }
        }

        /// <summary>
        /// Spawns floating damage/XP text at a 3D world position that drifts upward and fades out.
        /// </summary>
        public void SpawnFloatingText(Vector3 worldPos, string text, Color color, float size = 4.5f, bool isCritical = false)
        {
            StartCoroutine(FloatingTextRoutine(worldPos + Vector3.up * 1.8f, text, color, size, isCritical));
        }

        private IEnumerator FloatingTextRoutine(Vector3 startPos, string text, Color color, float fontSize, bool isCritical)
        {
            GameObject textGo = new GameObject("FloatingCombatText");
            textGo.transform.position = startPos;

            // Ensure text faces the camera
            if (targetCamera != null)
            {
                textGo.transform.rotation = targetCamera.transform.rotation;
            }

            TextMeshPro tmp = textGo.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = fontSize * (isCritical ? 1.4f : 1f);
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;

            float duration = isCritical ? 1.2f : 0.9f;
            float elapsed = 0f;
            Vector3 endPos = startPos + new Vector3(Random.Range(-0.3f, 0.3f), isCritical ? 1.6f : 1.1f, 0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Arc movement with ease out
                float easeOut = Mathf.Sin(t * Mathf.PI * 0.5f);
                textGo.transform.position = Vector3.Lerp(startPos, endPos, easeOut);

                // Punch scale on pop
                if (t < 0.2f)
                {
                    float scale = Mathf.Lerp(0.5f, isCritical ? 1.4f : 1.15f, t / 0.2f);
                    textGo.transform.localScale = Vector3.one * scale;
                }
                else
                {
                    textGo.transform.localScale = Vector3.one * Mathf.Lerp(isCritical ? 1.4f : 1.15f, 0.9f, (t - 0.2f) / 0.8f);
                }

                // Alpha fade out near end
                if (t > 0.5f)
                {
                    float alpha = Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
                    tmp.color = new Color(color.r, color.g, color.b, alpha);
                }

                // Keep facing camera
                if (targetCamera != null)
                {
                    textGo.transform.rotation = targetCamera.transform.rotation;
                }

                yield return null;
            }

            Destroy(textGo);
        }

        /// <summary>
        /// Triggers a punchy screen shake on the main camera.
        /// </summary>
        public void ShakeCamera(float intensity = 0.25f, float duration = 0.2f)
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                targetCamera.transform.localPosition = originalCamPos;
            }

            shakeCoroutine = StartCoroutine(ShakeCameraRoutine(intensity, duration));
        }

        private IEnumerator ShakeCameraRoutine(float intensity, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float currentIntensity = Mathf.Lerp(intensity, 0f, elapsed / duration);
                Vector3 offset = Random.insideUnitSphere * currentIntensity;
                offset.z = 0f; // Keep camera plane steady in 2.5D view
                targetCamera.transform.localPosition = originalCamPos + offset;
                yield return null;
            }

            targetCamera.transform.localPosition = originalCamPos;
            shakeCoroutine = null;
        }

        /// <summary>
        /// Zooms the camera in momentarily and punches FOV during critical strikes or finishers.
        /// </summary>
        public void TriggerActionCamZoom(float duration = 0.32f, float targetFOV = 38f)
        {
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera == null) return;

            if (zoomCoroutine != null)
            {
                StopCoroutine(zoomCoroutine);
                targetCamera.fieldOfView = originalFOV;
            }

            zoomCoroutine = StartCoroutine(ActionCamZoomRoutine(duration, targetFOV));
        }

        private IEnumerator ActionCamZoomRoutine(float duration, float targetFOV)
        {
            float startFOV = originalFOV > 0 ? originalFOV : targetCamera.fieldOfView;
            float halfTime = duration * 0.45f;
            float elapsed = 0f;

            while (elapsed < halfTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / halfTime;
                targetCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            elapsed = 0f;
            float returnTime = duration * 0.55f;
            while (elapsed < returnTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / returnTime;
                targetCamera.fieldOfView = Mathf.Lerp(targetFOV, startFOV, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            targetCamera.fieldOfView = startFOV;
            zoomCoroutine = null;
        }

        /// <summary>
        /// Emits a dynamic radial spark particle burst on weapon or magic impact.
        /// </summary>
        public void SpawnHitSparks(Vector3 worldPos, Color color, int count = 16)
        {
            StartCoroutine(HitSparksRoutine(worldPos, color, count));
        }

        private IEnumerator HitSparksRoutine(Vector3 worldPos, Color color, int count)
        {
            GameObject sparksRoot = new GameObject("HitSparksBurst");
            sparksRoot.transform.position = worldPos;

            // Generate small glowing spark fragments flying radially outward
            List<Transform> sparkBits = new List<Transform>();
            List<Vector3> velocities = new List<Vector3>();

            Material sparkMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Mobile/Particles/Additive"));
            if (sparkMat.HasProperty("_BaseColor")) sparkMat.SetColor("_BaseColor", color);
            else if (sparkMat.HasProperty("_Color")) sparkMat.color = color;

            for (int i = 0; i < count; i++)
            {
                GameObject bit = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bit.transform.SetParent(sparksRoot.transform, false);
                bit.transform.localScale = Vector3.one * Random.Range(0.08f, 0.16f);

                // Strip collider
                Collider col = bit.GetComponent<Collider>();
                if (col != null) Destroy(col);

                Renderer r = bit.GetComponent<Renderer>();
                if (r != null) r.material = sparkMat;

                sparkBits.Add(bit.transform);

                // Radial 3D velocity outward with bias towards camera/vertical
                Vector3 randDir = Random.insideUnitSphere.normalized;
                randDir.y = Mathf.Abs(randDir.y) * 1.2f + 0.3f;
                float speed = Random.Range(3.5f, 7.5f);
                velocities.Add(randDir * speed);
            }

            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float gravity = -9.8f * Time.deltaTime;

                for (int i = 0; i < sparkBits.Count; i++)
                {
                    if (sparkBits[i] == null) continue;
                    Vector3 vel = velocities[i];
                    vel.y += gravity;
                    velocities[i] = vel;

                    sparkBits[i].position += vel * Time.deltaTime;
                    sparkBits[i].localScale = Vector3.one * Mathf.Lerp(0.12f, 0.01f, t);
                    sparkBits[i].Rotate(Random.Range(200f, 600f) * Time.deltaTime, Random.Range(200f, 600f) * Time.deltaTime, 0f);
                }

                yield return null;
            }

            Destroy(sparksRoot);
        }

        /// <summary>
        /// Spawns a glowing math magic projectile that arcs from caster to target before exploding.
        /// </summary>
        public void SpawnSpellProjectile(Vector3 startPos, Vector3 targetPos, Color glowColor, System.Action onImpactCallback)
        {
            StartCoroutine(SpellProjectileRoutine(startPos, targetPos, glowColor, onImpactCallback));
        }

        private IEnumerator SpellProjectileRoutine(Vector3 startPos, Vector3 targetPos, Color glowColor, System.Action onImpactCallback)
        {
            GameObject proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            proj.name = "MathSpellProjectile";
            proj.transform.position = startPos;
            proj.transform.localScale = Vector3.one * 0.32f;

            Collider col = proj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Glowing material
            Material projMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Mobile/Particles/Additive") ?? Shader.Find("Sprites/Default"));
            if (projMat.HasProperty("_BaseColor")) projMat.SetColor("_BaseColor", glowColor);
            else if (projMat.HasProperty("_Color")) projMat.color = glowColor;

            Renderer r = proj.GetComponent<Renderer>();
            if (r != null) r.material = projMat;

            // Trail
            TrailRenderer trail = proj.AddComponent<TrailRenderer>();
            trail.material = projMat;
            trail.startWidth = 0.22f;
            trail.endWidth = 0.02f;
            trail.time = 0.22f;
            trail.startColor = glowColor;
            trail.endColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);

            float flightDuration = 0.28f;
            float elapsed = 0f;
            float arcHeight = 1.1f;

            while (elapsed < flightDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flightDuration;

                Vector3 current = Vector3.Lerp(startPos, targetPos, t);
                current.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                proj.transform.position = current;

                yield return null;
            }

            // On Impact:
            SpawnHitSparks(targetPos, glowColor, 18);
            ShakeCamera(0.2f, 0.18f);
            onImpactCallback?.Invoke();

            Destroy(proj);
        }
    }
}

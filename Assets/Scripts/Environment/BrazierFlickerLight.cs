using UnityEngine;

namespace DigitDuelist
{
    /// <summary>
    /// Adds dynamic, atmospheric fire flickering to torches and braziers in the arena.
    /// </summary>
    public class BrazierFlickerLight : MonoBehaviour
    {
        [Header("Light Settings")]
        [SerializeField] private Light targetLight;
        [SerializeField] private float minIntensity = 1.8f;
        [SerializeField] private float maxIntensity = 3.2f;
        [SerializeField] private float flickerSpeed = 8f;

        [Header("Color Warmth")]
        [SerializeField] private Color baseColor = new Color(1f, 0.58f, 0.22f, 1f);
        [SerializeField] private Color hotColor = new Color(1f, 0.8f, 0.4f, 1f);

        [Header("Positional Jitter")]
        [SerializeField] private bool jitterPosition = true;
        [SerializeField] private float jitterAmount = 0.04f;

        private Vector3 initialLocalPos;
        private float noiseOffset;

        private void Awake()
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light>() ?? GetComponentInChildren<Light>();
            }

            if (targetLight != null)
            {
                initialLocalPos = targetLight.transform.localPosition;
            }

            noiseOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (targetLight == null) return;

            float time = Time.time * flickerSpeed + noiseOffset;
            float perlin = Mathf.PerlinNoise(time, time * 0.5f);

            targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, perlin);
            targetLight.color = Color.Lerp(baseColor, hotColor, perlin);

            if (jitterPosition)
            {
                float xNoise = (Mathf.PerlinNoise(time * 1.5f, 0f) - 0.5f) * jitterAmount;
                float zNoise = (Mathf.PerlinNoise(0f, time * 1.5f) - 0.5f) * jitterAmount;
                targetLight.transform.localPosition = initialLocalPos + new Vector3(xNoise, 0f, zNoise);
            }
        }
    }
}

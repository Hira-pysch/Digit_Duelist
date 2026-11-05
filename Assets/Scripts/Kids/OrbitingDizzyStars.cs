using UnityEngine;
using TMPro;

namespace DigitDuelist
{
    /// <summary>
    /// Spawns a ring of whimsical cartoon orbiting stars or spirals around a character's head
    /// during playful defeat or stumble states, replacing violent knockouts with cartoon charm.
    /// </summary>
    public class OrbitingDizzyStars : MonoBehaviour
    {
        [Header("Orbit Settings")]
        [SerializeField] private int starCount = 4;
        [SerializeField] private float orbitRadius = 0.55f;
        [SerializeField] private float orbitSpeed = 220f;
        [SerializeField] private float verticalBobSpeed = 4f;
        [SerializeField] private float verticalBobHeight = 0.08f;
        [SerializeField] private Vector3 offsetAboveHead = new Vector3(0f, 2.1f, 0f);

        private GameObject[] starObjects;
        private Transform targetTransform;
        private bool isOrbiting = false;
        private float angleOffset = 0f;

        public void AttachToCharacter(Transform character)
        {
            targetTransform = character;
            CreateStars();
            isOrbiting = true;
        }

        private void CreateStars()
        {
            ClearStars();

            starObjects = new GameObject[starCount];
            for (int i = 0; i < starCount; i++)
            {
                GameObject starGo = new GameObject($"DizzyStar_{i}");
                starGo.transform.SetParent(this.transform);

                TextMeshPro tmp = starGo.AddComponent<TextMeshPro>();
                tmp.text = "⭐";
                tmp.fontSize = 4.5f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = new Color(1f, 0.9f, 0.2f);

                starObjects[i] = starGo;
            }
        }

        private void Update()
        {
            if (!isOrbiting || targetTransform == null || starObjects == null) return;

            angleOffset += orbitSpeed * Time.deltaTime;
            Vector3 center = targetTransform.position + offsetAboveHead;
            float bob = Mathf.Sin(Time.time * verticalBobSpeed) * verticalBobHeight;

            Camera mainCam = Camera.main;

            for (int i = 0; i < starObjects.Length; i++)
            {
                if (starObjects[i] == null) continue;

                float angle = angleOffset + (i * (360f / starCount));
                float rad = angle * Mathf.Deg2Rad;

                Vector3 pos = center + new Vector3(
                    Mathf.Cos(rad) * orbitRadius,
                    bob + Mathf.Sin(rad * 2f) * 0.05f,
                    Mathf.Sin(rad) * orbitRadius
                );

                starObjects[i].transform.position = pos;

                // Face the camera
                if (mainCam != null)
                {
                    starObjects[i].transform.rotation = mainCam.transform.rotation;
                }
            }
        }

        public void StopOrbit()
        {
            isOrbiting = false;
            ClearStars();
            Destroy(gameObject);
        }

        private void ClearStars()
        {
            if (starObjects != null)
            {
                foreach (var s in starObjects)
                {
                    if (s != null) Destroy(s);
                }
                starObjects = null;
            }
        }
    }
}

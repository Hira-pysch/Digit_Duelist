using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace DigitDuelist
{
    /// <summary>
    /// Manages stage backgrounds directly in numerical order (1, 2, 3, 4, 5, 6, 7).
    /// Each background lasts for 5 stages.
    /// When all backgrounds have been used, rotates back to the first background and loops continuously.
    /// Displays the background on a 3D Quad childed to Main Camera, cleanly replacing the grey skybox
    /// while keeping all character models, sprites, statue, and floor tiles exactly as they are.
    /// </summary>
    [ExecuteAlways]
    public class StageBackgroundManager : MonoBehaviour
    {
        public static StageBackgroundManager Instance { get; private set; }

        [Header("Background Materials (Direct in Numerical Order 1..7)")]
        [Tooltip("Plains_1, Forest_2, Mountain_3, Ice Mountain_4, Ruins_5, Hellscape_6, Dark Castle_7")]
        [SerializeField] private Material[] backgroundMaterials = new Material[0];

        [Header("Backdrop Renderer Reference")]
        [SerializeField] private MeshRenderer backdropRenderer;
        [SerializeField] private Camera targetCamera;

        [Header("Backdrop Sizing & Position")]
        [SerializeField] private float distanceFromCamera = 25f;
        [SerializeField] private float paddingMultiplier = 1.15f;

        [Header("Optional UI Image Binding")]
        [SerializeField] private Image uiBackgroundImage;

        [Header("Runtime State")]
        [SerializeField] private int activeStage = 1;
        [SerializeField] private int activeBackgroundIndex = 0;
        [SerializeField] private string activeBackgroundName = "";

        private GameObject backdropRoot;
        private int currentBackgroundIndex = -1;

        public int CurrentBackgroundIndex => currentBackgroundIndex;
        public Material[] BackgroundMaterials => backgroundMaterials;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                if (Instance == null) Instance = this;
                else if (Instance != this)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            EnsureCamera();
            EnsureMaterialsLoaded();
            EnsureBackdropQuad();
        }

        private void Start()
        {
            int level = 1;
            if (GameManager.Instance != null && GameManager.Instance.CurrentPlayerData != null)
            {
                level = GameManager.Instance.CurrentPlayerData.currentLevel;
            }
            UpdateBackgroundForStage(level, instant: true);
        }

        private void OnEnable()
        {
            EnsureCamera();
            EnsureMaterialsLoaded();
            EnsureBackdropQuad();
            if (activeStage > 0)
            {
                UpdateBackgroundForStage(activeStage, instant: true);
            }
        }

        private void LateUpdate()
        {
            // Keep backdrop perfectly framed to camera frustum at any aspect ratio
            AdjustBackdropFrustum();
        }

        private void OnValidate()
        {
            EnsureMaterialsLoaded();
            EnsureBackdropQuad();
            if (activeStage > 0)
            {
                UpdateBackgroundForStage(activeStage, instant: true);
            }
        }

        /// <summary>
        /// Context menu action to reload materials and update the backdrop.
        /// </summary>
        [ContextMenu("Reload Backgrounds From Unity Project")]
        public void ReloadBackgroundsContextMenu()
        {
            EnsureCamera();
            LoadMaterials();
            EnsureBackdropQuad();
            UpdateBackgroundForStage(activeStage > 0 ? activeStage : 1, instant: true);
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/Digit Duelist/Fix and Apply Stage Backgrounds", false, 10)]
        public static void FixAndApplyStageBackgroundsMenu()
        {
            StageBackgroundManager mgr = FindFirstObjectByType<StageBackgroundManager>();
            if (mgr == null)
            {
                GameObject managersGo = GameObject.Find("[GameManagers]");
                if (managersGo == null) managersGo = new GameObject("[GameManagers]");
                mgr = managersGo.AddComponent<StageBackgroundManager>();
            }

            mgr.EnsureCamera();
            mgr.LoadMaterials();
            mgr.EnsureBackdropQuad();
            mgr.UpdateBackgroundForStage(1, instant: true);
            UnityEditor.EditorUtility.SetDirty(mgr);
            if (mgr.backdropRenderer != null) UnityEditor.EditorUtility.SetDirty(mgr.backdropRenderer.gameObject);
            Debug.Log("<color=green>[StageBackgroundManager]</color> Successfully applied stage background Plains_1 to scene!");
        }
#endif

        private void EnsureCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            }
        }

        /// <summary>
        /// Loads all 7 stage background materials in strictly ascending numerical order:
        /// 1. Plains_1 -> 2. Forest_2 -> 3. Mountain_3 -> 4. Ice Mountain_4 -> 5. Ruins_5 -> 6. Hellscape_6 -> 7. Dark Castle_7
        /// </summary>
        public void EnsureMaterialsLoaded()
        {
            if (backgroundMaterials == null || backgroundMaterials.Length == 0)
            {
                LoadMaterials();
            }
        }

        /// <summary>
        /// Backward-compatible alias for legacy callers.
        /// </summary>
        public void LoadBackgroundSpritesDirectlyFromUnity()
        {
            EnsureMaterialsLoaded();
        }

        private void LoadMaterials()
        {
            List<Material> list = new List<Material>();

            // 1. Direct from Resources/Background/Materials/
            Material[] loadedMats = Resources.LoadAll<Material>("Background/Materials");
            if (loadedMats != null && loadedMats.Length > 0)
            {
                foreach (var m in loadedMats)
                {
                    if (m != null && !list.Contains(m)) list.Add(m);
                }
            }

#if UNITY_EDITOR
            // 2. Scan Assets/Resources/Background/Materials/ or Assets/Background/
            if (list.Count == 0)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Material", new[] { "Assets/Resources/Background/Materials" });
                if (guids != null)
                {
                    foreach (string g in guids)
                    {
                        string p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
                        Material m = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(p);
                        if (m != null && !list.Contains(m)) list.Add(m);
                    }
                }
            }
#endif

            // Sort strictly in ascending numerical order by the number in each asset name
            list.Sort((a, b) =>
            {
                int orderA = ExtractOrderNumber(a != null ? a.name : "");
                int orderB = ExtractOrderNumber(b != null ? b.name : "");
                return orderA.CompareTo(orderB);
            });

            backgroundMaterials = list.ToArray();

            if (backgroundMaterials.Length > 0)
            {
                Debug.Log($"<color=cyan>[StageBackgroundManager]</color> Successfully loaded {backgroundMaterials.Length} background materials in numerical order:\n" +
                          string.Join("\n", Array.ConvertAll(backgroundMaterials, m => $"  [{ExtractOrderNumber(m.name)}] -> {m.name}")));
            }
        }

        /// <summary>
        /// Extracts the numerical order from an asset name (e.g. "Plains_1" -> 1, "Dark Castle_7" -> 7).
        /// </summary>
        public static int ExtractOrderNumber(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return int.MaxValue;

            Match match = Regex.Match(assetName, @"(?:_|\s)(\d+)(?:\D*)$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int order))
            {
                return order;
            }

            Match anyMatch = Regex.Match(assetName, @"\d+");
            if (anyMatch.Success && int.TryParse(anyMatch.Value, out int fallbackOrder))
            {
                return fallbackOrder;
            }

            return int.MaxValue;
        }

        /// <summary>
        /// Calculates the background index for a given stage number.
        /// Each background lasts 5 stages, advances in numerical order (1 -> 2 -> 3 -> 4 -> 5 -> 6 -> 7),
        /// and returns back to the first background once all have been used.
        /// </summary>
        public static int GetBackgroundIndexForStage(int stageNumber, int totalBackgrounds)
        {
            if (stageNumber < 1) stageNumber = 1;
            if (totalBackgrounds <= 0) return 0;

            int blockIndex = (stageNumber - 1) / 5;
            return blockIndex % totalBackgrounds;
        }

        /// <summary>
        /// Updates the stage background based on the current stage/level.
        /// </summary>
        public void UpdateBackgroundForStage(int stageNumber, bool instant = false)
        {
            EnsureMaterialsLoaded();
            if (backgroundMaterials == null || backgroundMaterials.Length == 0) return;

            int total = backgroundMaterials.Length;
            int targetIndex = GetBackgroundIndexForStage(stageNumber, total);

            activeStage = stageNumber;
            activeBackgroundIndex = targetIndex;

            Material targetMat = backgroundMaterials[targetIndex];
            if (targetMat == null) return;

            activeBackgroundName = targetMat.name;
            currentBackgroundIndex = targetIndex;

            EnsureBackdropQuad();

            if (backdropRenderer != null)
            {
                backdropRenderer.sharedMaterial = targetMat;
            }

            // Update UI Image if assigned
            if (uiBackgroundImage != null && targetMat.mainTexture != null)
            {
                Texture2D tex = targetMat.mainTexture as Texture2D;
                if (tex != null)
                {
                    uiBackgroundImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }

            Debug.Log($"<color=green>[StageBackgroundManager]</color> Stage {stageNumber} -> Active Background [{targetIndex + 1}/{total}]: {targetMat.name}");
        }

        /// <summary>
        /// Ensures the [Stage_Backdrop] Quad GameObject is properly set up, childed to Main Camera,
        /// and assigned the proper mesh and material.
        /// </summary>
        public void EnsureBackdropQuad()
        {
            EnsureCamera();
            if (targetCamera == null) return;

            if (backdropRenderer != null && backdropRenderer.gameObject != null)
            {
                backdropRoot = backdropRenderer.gameObject;
            }
            else
            {
                backdropRoot = GameObject.Find("[Stage_Backdrop]");
                if (backdropRoot == null)
                {
                    // Search inside targetCamera children
                    Transform childT = targetCamera.transform.Find("[Stage_Backdrop]");
                    if (childT != null) backdropRoot = childT.gameObject;
                }

                if (backdropRoot == null)
                {
                    backdropRoot = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    backdropRoot.name = "[Stage_Backdrop]";
                    Collider col = backdropRoot.GetComponent<Collider>();
                    if (col != null) DestroyImmediate(col);
                }

                backdropRenderer = backdropRoot.GetComponent<MeshRenderer>();
                if (backdropRenderer == null)
                {
                    backdropRenderer = backdropRoot.AddComponent<MeshRenderer>();
                }
            }

            // Ensure parented to camera
            if (backdropRoot.transform.parent != targetCamera.transform)
            {
                backdropRoot.transform.SetParent(targetCamera.transform, false);
            }

            // Position directly along camera forward axis at distanceFromCamera
            backdropRoot.transform.localPosition = new Vector3(0f, 0f, distanceFromCamera);
            backdropRoot.transform.localRotation = Quaternion.identity;

            // Apply active material if currently unassigned
            if (backdropRenderer.sharedMaterial == null && backgroundMaterials != null && backgroundMaterials.Length > 0)
            {
                int idx = GetBackgroundIndexForStage(activeStage > 0 ? activeStage : 1, backgroundMaterials.Length);
                backdropRenderer.sharedMaterial = backgroundMaterials[idx];
            }

            AdjustBackdropFrustum();
        }

        private void AdjustBackdropFrustum()
        {
            if (targetCamera == null || backdropRoot == null) return;

            float fov = targetCamera.fieldOfView;
            float aspect = targetCamera.aspect;
            if (aspect <= 0.01f) aspect = 16f / 9f;

            float frustumHeight = 2f * distanceFromCamera * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float frustumWidth = frustumHeight * aspect;

            // Apply scale with padding multiplier to prevent any boundary clipping
            Vector3 targetScale = new Vector3(frustumWidth * paddingMultiplier, frustumHeight * paddingMultiplier, 1f);
            if (backdropRoot.transform.localScale != targetScale)
            {
                backdropRoot.transform.localScale = targetScale;
            }

            if (backdropRoot.transform.localPosition != new Vector3(0f, 0f, distanceFromCamera))
            {
                backdropRoot.transform.localPosition = new Vector3(0f, 0f, distanceFromCamera);
            }

            if (backdropRoot.transform.localRotation != Quaternion.identity)
            {
                backdropRoot.transform.localRotation = Quaternion.identity;
            }
        }
    }
}

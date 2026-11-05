using System;
using System.Collections.Generic;
using UnityEngine;

namespace DigitDuelist
{
    [Serializable]
    public class KnightSkinData
    {
        public string skinId;
        public string displayName;
        public string description;
        public int requiredStreak = 0;
        public int requiredLevel = 1;
        public string prefabPath;
        public bool isUnlocked = false;
    }

    /// <summary>
    /// Manages cosmetic skins and armor unlocks for young players.
    /// Rewards effort, streaks, and level progression with exciting visual customization.
    /// </summary>
    public class SkinCustomizationManager : MonoBehaviour
    {
        public static SkinCustomizationManager Instance { get; private set; }

        [Header("Available Skins")]
        [SerializeField] private List<KnightSkinData> availableSkins = new List<KnightSkinData>()
        {
            new KnightSkinData
            {
                skinId = "knight_blue",
                displayName = "🛡️ Cobalt Knight",
                description = "Starter brave knight armor.",
                requiredStreak = 0,
                requiredLevel = 1,
                prefabPath = "Assets/SyntyStudios/PolygonKnights/Prefabs/Characters/Character_Knight_01_Blue.prefab",
                isUnlocked = true
            },
            new KnightSkinData
            {
                skinId = "knight_orange",
                displayName = "🔥 Solar Flare Knight",
                description = "Unlocked by reaching a 3-Answer Streak or Level 2!",
                requiredStreak = 3,
                requiredLevel = 2,
                prefabPath = "Assets/SyntyStudios/PolygonKnights/Prefabs/Characters/Character_Knight_01_Orange.prefab",
                isUnlocked = false
            },
            new KnightSkinData
            {
                skinId = "knight_yellow",
                displayName = "✨ Golden Paladin",
                description = "Unlocked by reaching a 5-Answer Streak or Level 4!",
                requiredStreak = 5,
                requiredLevel = 4,
                prefabPath = "Assets/SyntyStudios/PolygonKnights/Prefabs/Characters/Character_Knight_01_Yellow.prefab",
                isUnlocked = false
            },
            new KnightSkinData
            {
                skinId = "knight_black",
                displayName = "⚔️ Obsidian Champion",
                description = "Unlocked by reaching Level 6!",
                requiredStreak = 7,
                requiredLevel = 6,
                prefabPath = "Assets/SyntyStudios/PolygonKnights/Prefabs/Characters/Character_Knight_01_Black.prefab",
                isUnlocked = false
            }
        };

        [Header("Current Equipped Skin")]
        [SerializeField] private string currentEquippedSkinId = "knight_blue";

        // Events
        public event Action<KnightSkinData> OnSkinUnlocked;
        public event Action<KnightSkinData> OnSkinEquipped;

        public List<KnightSkinData> AvailableSkins => availableSkins;
        public string CurrentEquippedSkinId => currentEquippedSkinId;

        private const string PrefKey_EquippedSkin = "DigitDuelist_EquippedSkin";
        private const string PrefKey_SkinPrefix = "DigitDuelist_SkinUnlocked_";

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            LoadSkins();
        }

        private void LoadSkins()
        {
            currentEquippedSkinId = PlayerPrefs.GetString(PrefKey_EquippedSkin, "knight_blue");

            foreach (var skin in availableSkins)
            {
                if (skin.skinId == "knight_blue")
                {
                    skin.isUnlocked = true;
                }
                else
                {
                    skin.isUnlocked = PlayerPrefs.GetInt(PrefKey_SkinPrefix + skin.skinId, 0) == 1;
                }
            }
        }

        /// <summary>
        /// Evaluates player achievements and unlocks eligible skins.
        /// </summary>
        public void CheckUnlocks(int currentLevel, int highestStreak)
        {
            foreach (var skin in availableSkins)
            {
                if (!skin.isUnlocked)
                {
                    bool streakMet = (skin.requiredStreak > 0 && highestStreak >= skin.requiredStreak);
                    bool levelMet = (skin.requiredLevel > 0 && currentLevel >= skin.requiredLevel);

                    if (streakMet || levelMet)
                    {
                        skin.isUnlocked = true;
                        PlayerPrefs.SetInt(PrefKey_SkinPrefix + skin.skinId, 1);
                        PlayerPrefs.Save();

                        OnSkinUnlocked?.Invoke(skin);

                        if (KidAudioManager.Instance != null) KidAudioManager.Instance.PlayStreakFanfare();

                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.Notify($"🎉 NEW ARMOR UNLOCKED: {skin.displayName}!");
                        }
                    }
                }
            }
        }

        public bool EquipSkin(string skinId)
        {
            KnightSkinData skin = availableSkins.Find(s => s.skinId == skinId);
            if (skin == null || !skin.isUnlocked) return false;

            currentEquippedSkinId = skinId;
            PlayerPrefs.SetString(PrefKey_EquippedSkin, skinId);
            PlayerPrefs.Save();

            ApplySkinToPlayer(skin);
            OnSkinEquipped?.Invoke(skin);
            return true;
        }

        private void ApplySkinToPlayer(KnightSkinData skin)
        {
            GameObject playerGo = GameObject.Find("Player_Knight");
            if (playerGo == null) return;

#if UNITY_EDITOR
            // Replace mesh in editor / runtime if prefab available
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(skin.prefabPath);
            if (prefab != null)
            {
                // Swap out visual children
                Transform visualsChild = playerGo.transform.Find("CharacterMesh");
                if (visualsChild != null)
                {
                    Destroy(visualsChild.gameObject);
                }

                GameObject newMesh = Instantiate(prefab, playerGo.transform);
                newMesh.name = "CharacterMesh";
                newMesh.transform.localPosition = Vector3.zero;
                newMesh.transform.localRotation = Quaternion.identity;
            }
#endif
        }
    }
}

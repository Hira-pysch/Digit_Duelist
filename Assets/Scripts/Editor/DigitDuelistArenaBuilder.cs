#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DigitDuelist.Editor
{
    public class DigitDuelistArenaBuilder : EditorWindow
    {
        [MenuItem("Tools/Digit Duelist/Build 3D Arena & Characters", false, 2)]
        public static void BuildCompleteArena()
        {
            // 1. Setup Lighting
            SetupArenaLighting();

            // 2. Setup Camera
            SetupArenaCamera();

            // 3. Build Environment / Map
            GameObject arenaRoot = GameObject.Find("[Arena_Environment]");
            if (arenaRoot != null)
            {
                Undo.DestroyObjectImmediate(arenaRoot);
            }
            arenaRoot = new GameObject("[Arena_Environment]");
            Undo.RegisterCreatedObjectUndo(arenaRoot, "Create [Arena_Environment]");

            BuildArenaMap(arenaRoot.transform);

            // 4. Setup Characters
            GameObject playerGo = SetupCharacter(
                "Player_Knight",
                "Assets/SyntyStudios/PolygonKnights/Prefabs/Characters/Character_Knight_01_Blue.prefab",
                new Vector3(-3.5f, 0f, 0f),
                Quaternion.Euler(0f, 85f, 0f),
                isPlayer: true,
                "Assets/SyntyStudios/PolygonKnights/Prefabs/Weapons/SM_Wep_Broadsword_01.prefab",
                "Assets/SyntyStudios/PolygonKnights/Prefabs/Weapons/SM_Wep_Shield_01.prefab"
            );

            GameObject enemyGo = SetupCharacter(
                "Enemy_Challenger",
                "Assets/SyntyStudios/PolygonKnights/Prefabs/Characters/Character_Soldier_01_Black.prefab",
                new Vector3(3.5f, 0f, 0f),
                Quaternion.Euler(0f, -85f, 0f),
                isPlayer: false,
                "Assets/SyntyStudios/PolygonKnights/Prefabs/Weapons/SM_Wep_Halberd_01.prefab",
                null
            );

            // 5. Ensure Managers & CombatFXController exist
            GameObject managersGo = GameObject.Find("[GameManagers]");
            if (managersGo == null)
            {
                managersGo = new GameObject("[GameManagers]");
                Undo.RegisterCreatedObjectUndo(managersGo, "Create GameManagers");
            }

            GameManager gm = managersGo.GetComponent<GameManager>() ?? Undo.AddComponent<GameManager>(managersGo);
            DuelController duel = managersGo.GetComponent<DuelController>() ?? Undo.AddComponent<DuelController>(managersGo);
            MathGenerator math = managersGo.GetComponent<MathGenerator>() ?? Undo.AddComponent<MathGenerator>(managersGo);
            UIManager ui = managersGo.GetComponent<UIManager>() ?? Undo.AddComponent<UIManager>(managersGo);
            CombatFXController fx = managersGo.GetComponent<CombatFXController>() ?? Undo.AddComponent<CombatFXController>(managersGo);

            if (playerGo != null && enemyGo != null)
            {
                CharacterDuelVisuals pVis = playerGo.GetComponent<CharacterDuelVisuals>();
                CharacterDuelVisuals eVis = enemyGo.GetComponent<CharacterDuelVisuals>();

                SetPrivateField(duel, "playerVisuals", pVis);
                SetPrivateField(duel, "enemyVisuals", eVis);
            }

            EditorUtility.SetDirty(duel);
            EditorUtility.SetDirty(managersGo);

            Debug.Log("<color=green>[DigitDuelist]</color> 3D Arena, Synty Characters, Lighting & Visual Combat Systems successfully built!");
            EditorUtility.DisplayDialog(
                "Digit Duelist 3D Arena Ready",
                "The 3D Arena map, castle backdrop, flickering braziers, Synty Knight player, enemy challenger, and combat animations have been generated and connected!",
                "Awesome!"
            );
        }

        private static void SetupArenaLighting()
        {
            Light dirLight = null;
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    dirLight = l;
                    break;
                }
            }

            if (dirLight == null)
            {
                GameObject lightGo = new GameObject("Directional Light");
                dirLight = lightGo.AddComponent<Light>();
                dirLight.type = LightType.Directional;
                Undo.RegisterCreatedObjectUndo(lightGo, "Create Directional Light");
            }

            dirLight.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            dirLight.color = new Color(1f, 0.95f, 0.88f, 1f);
            dirLight.intensity = 1.3f;
            dirLight.shadows = LightShadows.Soft;
        }

        private static void SetupArenaCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
                Undo.RegisterCreatedObjectUndo(camGo, "Create Main Camera");
            }

            cam.orthographic = false;
            cam.fieldOfView = 48f;
            cam.transform.position = new Vector3(0f, 2.5f, -8f);
            cam.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        }

        private static void BuildArenaMap(Transform parent)
        {
            // 1. Cobblestone Arena Floors
            string floorPrefabPath = "Assets/SyntyStudios/PolygonKnights/Prefabs/Environments/SM_Env_Path_Cobble_01.prefab";
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(floorPrefabPath))
            {
                floorPrefabPath = "Assets/SyntyStudios/PolygonKnights/Prefabs/Environments/SM_Env_Path_Tile_01.prefab";
            }

            Vector3[] floorPositions = new Vector3[]
            {
                new Vector3(-6f, 0f, 0f),
                new Vector3(-2f, 0f, 0f),
                new Vector3(2f, 0f, 0f),
                new Vector3(6f, 0f, 0f),
                new Vector3(-4f, 0f, -2.5f),
                new Vector3(0f, 0f, -2.5f),
                new Vector3(4f, 0f, -2.5f),
                new Vector3(-4f, 0f, 2.5f),
                new Vector3(0f, 0f, 2.5f),
                new Vector3(4f, 0f, 2.5f)
            };

            foreach (var pos in floorPositions)
            {
                InstantiateProp(floorPrefabPath, pos, Quaternion.identity, new Vector3(2f, 1f, 2f), parent, "Floor");
            }

            // 2. Castle Back Walls & Pillars
            string wallPrefabPath = "Assets/SyntyStudios/PolygonKnights/Prefabs/Buildings/SM_Bld_Castle_Wall_01.prefab";
            InstantiateProp(wallPrefabPath, new Vector3(-6f, 0f, 4.5f), Quaternion.identity, Vector3.one, parent, "BackWall_Left");
            InstantiateProp(wallPrefabPath, new Vector3(6f, 0f, 4.5f), Quaternion.identity, Vector3.one, parent, "BackWall_Right");

            string doorPrefabPath = "Assets/SyntyStudios/PolygonKnights/Prefabs/Buildings/SM_Bld_Castle_Door_01.prefab";
            InstantiateProp(doorPrefabPath, new Vector3(0f, 0f, 4.8f), Quaternion.identity, new Vector3(1.2f, 1.2f, 1.2f), parent, "CastleDoor");

            // 3. Central Knight Statue
            string statuePrefabPath = "Assets/SyntyStudios/PolygonKnights/Prefabs/Props/SM_Prop_Statue_01.prefab";
            InstantiateProp(statuePrefabPath, new Vector3(0f, 0f, 3.2f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.3f, 1.3f, 1.3f), parent, "HeroStatue");

            // 4. Banners
            string banner1Path = "Assets/SyntyStudios/PolygonKnights/Prefabs/Props/SM_Prop_Banner_01.prefab";
            string banner2Path = "Assets/SyntyStudios/PolygonKnights/Prefabs/Props/SM_Prop_Banner_02.prefab";
            InstantiateProp(banner1Path, new Vector3(-3.2f, 1.8f, 4.4f), Quaternion.identity, Vector3.one, parent, "Banner_Left");
            InstantiateProp(banner2Path, new Vector3(3.2f, 1.8f, 4.4f), Quaternion.identity, Vector3.one, parent, "Banner_Right");

            // 5. Fire Braziers with dynamic flickering light
            string brazierPath = "Assets/SyntyStudios/PolygonKnights/Prefabs/Props/SM_Prop_Brazier_01.prefab";
            CreateBrazier(brazierPath, new Vector3(-6.2f, 0f, 1.5f), parent, "Brazier_Left");
            CreateBrazier(brazierPath, new Vector3(6.2f, 0f, 1.5f), parent, "Brazier_Right");
        }

        private static GameObject CreateBrazier(string prefabPath, Vector3 pos, Transform parent, string name)
        {
            GameObject brazier = InstantiateProp(prefabPath, pos, Quaternion.identity, Vector3.one, parent, name);
            if (brazier != null)
            {
                // Add point light
                GameObject lightGo = new GameObject("FlameLight");
                lightGo.transform.SetParent(brazier.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, 1.2f, 0f);

                Light ptLight = lightGo.AddComponent<Light>();
                ptLight.type = LightType.Point;
                ptLight.range = 8f;
                ptLight.intensity = 2.4f;
                ptLight.color = new Color(1f, 0.6f, 0.25f, 1f);

                brazier.AddComponent<BrazierFlickerLight>();
            }
            return brazier;
        }

        private static GameObject SetupCharacter(string name, string characterPrefabPath, Vector3 pos, Quaternion rot, bool isPlayer, string rightWeaponPath, string leftWeaponPath)
        {
            // Remove existing if present
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
            }

            GameObject charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(characterPrefabPath);
            GameObject charInstance = null;

            if (charPrefab != null)
            {
                charInstance = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab);
                charInstance.name = name;
                charInstance.transform.position = pos;
                charInstance.transform.rotation = rot;
                Undo.RegisterCreatedObjectUndo(charInstance, $"Create {name}");
            }
            else
            {
                charInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                charInstance.name = name;
                charInstance.transform.position = pos;
                charInstance.transform.rotation = rot;
                Undo.RegisterCreatedObjectUndo(charInstance, $"Create {name}");
            }

            // Find Hand bones or create sockets
            Transform rightHandSocket = FindBone(charInstance.transform, "Hand_R", "hand_r", "RightHand", "Right_Hand", "Hand.R");
            if (rightHandSocket == null)
            {
                GameObject rSocketGo = new GameObject("RightHandSocket");
                rSocketGo.transform.SetParent(charInstance.transform, false);
                rSocketGo.transform.localPosition = isPlayer ? new Vector3(0.35f, 0.9f, 0.4f) : new Vector3(0.35f, 0.9f, 0.4f);
                rSocketGo.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                rightHandSocket = rSocketGo.transform;
            }

            Transform leftHandSocket = FindBone(charInstance.transform, "Hand_L", "hand_l", "LeftHand", "Left_Hand", "Hand.L");
            if (leftHandSocket == null)
            {
                GameObject lSocketGo = new GameObject("LeftHandSocket");
                lSocketGo.transform.SetParent(charInstance.transform, false);
                lSocketGo.transform.localPosition = isPlayer ? new Vector3(-0.35f, 0.9f, 0.3f) : new Vector3(-0.35f, 0.9f, 0.3f);
                leftHandSocket = lSocketGo.transform;
            }

            // Attach Weapons
            Transform rightWeaponInstance = null;
            if (!string.IsNullOrEmpty(rightWeaponPath))
            {
                GameObject wepPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rightWeaponPath);
                if (wepPrefab != null)
                {
                    GameObject wepGo = (GameObject)PrefabUtility.InstantiatePrefab(wepPrefab);
                    wepGo.transform.SetParent(rightHandSocket, false);
                    wepGo.transform.localPosition = Vector3.zero;
                    wepGo.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    rightWeaponInstance = wepGo.transform;
                }
            }

            Transform leftWeaponInstance = null;
            if (!string.IsNullOrEmpty(leftWeaponPath))
            {
                GameObject shieldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(leftWeaponPath);
                if (shieldPrefab != null)
                {
                    GameObject shieldGo = (GameObject)PrefabUtility.InstantiatePrefab(shieldPrefab);
                    shieldGo.transform.SetParent(leftHandSocket, false);
                    shieldGo.transform.localPosition = Vector3.zero;
                    shieldGo.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    leftWeaponInstance = shieldGo.transform;
                }
            }

            // Attach CharacterDuelVisuals
            CharacterDuelVisuals visuals = charInstance.GetComponent<CharacterDuelVisuals>() ?? Undo.AddComponent<CharacterDuelVisuals>(charInstance);
            SetPrivateField(visuals, "isPlayer", isPlayer);
            SetPrivateField(visuals, "weaponRightHand", rightWeaponInstance ?? rightHandSocket);
            SetPrivateField(visuals, "shieldLeftHand", leftWeaponInstance ?? leftHandSocket);

            EditorUtility.SetDirty(visuals);
            EditorUtility.SetDirty(charInstance);

            return charInstance;
        }

        private static Transform FindBone(Transform root, params string[] boneNames)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                foreach (var name in boneNames)
                {
                    if (child.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return child;
                    }
                }
            }
            return null;
        }

        private static GameObject InstantiateProp(string path, Vector3 pos, Quaternion rot, Vector3 scale, Transform parent, string fallbackName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance = null;

            if (prefab != null)
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.SetParent(parent, false);
                instance.transform.position = pos;
                instance.transform.rotation = rot;
                instance.transform.localScale = scale;
                Undo.RegisterCreatedObjectUndo(instance, $"Create {instance.name}");
            }
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                instance.name = fallbackName;
                instance.transform.SetParent(parent, false);
                instance.transform.position = pos;
                instance.transform.rotation = rot;
                instance.transform.localScale = scale;
                Undo.RegisterCreatedObjectUndo(instance, $"Create {fallbackName}");
            }

            return instance;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fi != null)
            {
                fi.SetValue(target, value);
            }
        }
    }
}
#endif

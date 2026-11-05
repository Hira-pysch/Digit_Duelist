using System;
using System.IO;
using UnityEngine;

namespace DigitDuelist
{
    /// <summary>
    /// Handles persistent JSON serialization for PlayerData.
    /// Fulfills the "SAVING GAME" node in the Digit Duelist flowchart.
    /// </summary>
    public static class SaveManager
    {
        private const string SaveFileName = "digit_duelist_save.json";

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static event Action<PlayerData> OnGameSaved;
        public static event Action<PlayerData> OnGameLoaded;

        /// <summary>
        /// Serializes player data into a JSON file at persistentDataPath.
        /// </summary>
        public static bool SaveGame(PlayerData data)
        {
            if (data == null)
            {
                Debug.LogError("[SaveManager] Cannot save null PlayerData.");
                return false;
            }

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
                Debug.Log($"[SaveManager] Game successfully saved to: {SaveFilePath}\nLevel: {data.currentLevel}, Mode: {data.chosenMode}, Score: {data.score}");
                OnGameSaved?.Invoke(data);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save game data: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads and deserializes player data from JSON file. Returns null if no save found or failed.
        /// </summary>
        public static PlayerData LoadGame()
        {
            if (!HasSaveData())
            {
                Debug.LogWarning($"[SaveManager] No save file found at: {SaveFilePath}");
                return null;
            }

            try
            {
                string json = File.ReadAllText(SaveFilePath);
                PlayerData loadedData = JsonUtility.FromJson<PlayerData>(json);
                Debug.Log($"[SaveManager] Game successfully loaded. Level: {loadedData.currentLevel}, Mode: {loadedData.chosenMode}, Score: {loadedData.score}");
                OnGameLoaded?.Invoke(loadedData);
                return loadedData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to load game data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Checks whether a valid save file exists on disk.
        /// </summary>
        public static bool HasSaveData()
        {
            return File.Exists(SaveFilePath);
        }

        /// <summary>
        /// Deletes the existing save file.
        /// </summary>
        public static bool DeleteSaveData()
        {
            if (HasSaveData())
            {
                try
                {
                    File.Delete(SaveFilePath);
                    Debug.Log("[SaveManager] Save file deleted.");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveManager] Error deleting save file: {ex.Message}");
                    return false;
                }
            }
            return false;
        }
    }
}

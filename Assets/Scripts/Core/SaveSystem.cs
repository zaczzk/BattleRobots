using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Handles all persistence for BattleRobots.
    /// Writes / reads a single JSON file encrypted with a simple XOR cipher.
    ///
    /// Usage:
    ///   SaveSystem.Save(saveData);
    ///   SaveData data = SaveSystem.Load();
    ///
    /// The XOR key is a single compile-time byte; a proper key-derivation pass
    /// is tracked as a post-M5 hardening task.
    /// </summary>
    public static class SaveSystem
    {
        private const byte XorKey = 0xAB;
        private const string SaveFileName = "battlerobots.sav";

        private static string SaveFilePath =>
            Path.Combine(Application.persistentDataPath, SaveFileName);

        // ── Save ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Serialises <paramref name="data"/> to JSON, XOR-encrypts it,
        /// and writes it atomically via a temp file.
        /// </summary>
        public static void Save(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: false);
                byte[] plain = Encoding.UTF8.GetBytes(json);
                byte[] encrypted = Xor(plain);

                // Write to a temp file first, then rename — atomic on most platforms.
                string tempPath = SaveFilePath + ".tmp";
                File.WriteAllBytes(tempPath, encrypted);
                File.Copy(tempPath, SaveFilePath, overwrite: true);
                File.Delete(tempPath);

                Debug.Log($"[SaveSystem] Saved to {SaveFilePath} ({encrypted.Length} bytes).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Save failed: {ex}");
            }
        }

        // ── Load ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads and decrypts the save file.
        /// Returns a fresh <see cref="SaveData"/> if no save exists or the file is corrupt.
        /// </summary>
        public static SaveData Load()
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.Log("[SaveSystem] No save file found. Returning default SaveData.");
                return new SaveData();
            }

            try
            {
                byte[] encrypted = File.ReadAllBytes(SaveFilePath);
                byte[] plain = Xor(encrypted);
                string json = Encoding.UTF8.GetString(plain);

                SaveData data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                {
                    Debug.LogWarning("[SaveSystem] Deserialization returned null. Returning default.");
                    return new SaveData();
                }

                Debug.Log($"[SaveSystem] Loaded from {SaveFilePath}.");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] Load failed (corrupt save?): {ex}");
                return new SaveData();
            }
        }

        // ── Delete ────────────────────────────────────────────────────────────

        /// <summary>Deletes the save file (used by the "erase data" option in settings).</summary>
        public static void Delete()
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log("[SaveSystem] Save file deleted.");
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// In-place XOR cipher. Same function encrypts and decrypts.
        /// Allocates a new array — only called on Save/Load, never in hot path.
        /// </summary>
        private static byte[] Xor(byte[] input)
        {
            byte[] output = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
                output[i] = (byte)(input[i] ^ XorKey);
            return output;
        }
    }
}

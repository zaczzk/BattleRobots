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
    ///   // Replay export (T072):
    ///   ReplayData rd = ReplayData.FromReplaySO(replaySO, matchRecord);
    ///   SaveSystem.SaveReplay(rd);
    ///   ReplayData loaded = SaveSystem.LoadReplay(matchRecord.timestamp);
    ///
    /// The XOR key is a single compile-time byte; a proper key-derivation pass
    /// is tracked as a post-M5 hardening task.
    /// </summary>
    public static class SaveSystem
    {
        private const byte XorKey = 0xAB;
        private const string SaveFileName = "battlerobots.sav";

        // Replay files are stored alongside the main save file.
        // Name format: replay_<sanitized-timestamp>.sav
        private const string ReplayFilePrefix = "replay_";
        private const string ReplayFileSuffix = ".sav";

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

        // ── Replay export (T072) ──────────────────────────────────────────────

        /// <summary>
        /// Serialises <paramref name="data"/> to JSON, XOR-encrypts it, and writes it
        /// atomically to a per-match replay file.
        ///
        /// File name is derived from <see cref="ReplayData.matchTimestamp"/> so each
        /// match has exactly one replay file. An existing replay for the same timestamp
        /// is silently overwritten.
        /// </summary>
        /// <param name="data">Must not be null.</param>
        public static void SaveReplay(ReplayData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            try
            {
                string path     = ReplayFilePath(data.matchTimestamp);
                string json     = JsonUtility.ToJson(data, prettyPrint: false);
                byte[] plain    = Encoding.UTF8.GetBytes(json);
                byte[] encrypted = Xor(plain);

                string tempPath = path + ".tmp";
                File.WriteAllBytes(tempPath, encrypted);
                File.Copy(tempPath, path, overwrite: true);
                File.Delete(tempPath);

                Debug.Log($"[SaveSystem] Replay saved to {path} ({encrypted.Length} bytes, " +
                          $"{data.frames.Count} frames).");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] SaveReplay failed: {ex}");
            }
        }

        /// <summary>
        /// Loads the replay file associated with <paramref name="matchTimestamp"/>.
        /// Returns <c>null</c> if no replay file exists for that timestamp or the
        /// file is corrupt (does not return a default object, unlike <see cref="Load"/>).
        /// </summary>
        /// <param name="matchTimestamp">
        /// The timestamp string from the corresponding <see cref="MatchRecord.timestamp"/>.
        /// </param>
        /// <returns>The deserialized <see cref="ReplayData"/>, or <c>null</c> on failure.</returns>
        public static ReplayData LoadReplay(string matchTimestamp)
        {
            string path = ReplayFilePath(matchTimestamp);

            if (!File.Exists(path))
            {
                Debug.Log($"[SaveSystem] No replay file for timestamp '{matchTimestamp}'.");
                return null;
            }

            try
            {
                byte[]     encrypted = File.ReadAllBytes(path);
                byte[]     plain     = Xor(encrypted);
                string     json      = Encoding.UTF8.GetString(plain);
                ReplayData data      = JsonUtility.FromJson<ReplayData>(json);

                if (data == null)
                {
                    Debug.LogWarning($"[SaveSystem] Replay deserialization returned null for '{matchTimestamp}'.");
                    return null;
                }

                Debug.Log($"[SaveSystem] Replay loaded from {path} ({data.frames.Count} frames).");
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSystem] LoadReplay failed for '{matchTimestamp}': {ex}");
                return null;
            }
        }

        /// <summary>
        /// Deletes the replay file for <paramref name="matchTimestamp"/> if it exists.
        /// Silent no-op when no file is found.
        /// </summary>
        public static void DeleteReplay(string matchTimestamp)
        {
            string path = ReplayFilePath(matchTimestamp);
            if (File.Exists(path))
            {
                File.Delete(path);
                Debug.Log($"[SaveSystem] Replay deleted: {path}.");
            }
        }

        /// <summary>
        /// Returns <c>true</c> if a replay file exists for the given match timestamp.
        /// </summary>
        public static bool ReplayExists(string matchTimestamp) =>
            File.Exists(ReplayFilePath(matchTimestamp));

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

        /// <summary>
        /// Converts a match timestamp into a safe file-system path for the replay file.
        /// Characters that are invalid on Windows/macOS (colon, period, plus) are
        /// replaced with hyphens.
        /// </summary>
        private static string ReplayFilePath(string matchTimestamp)
        {
            // Replace filesystem-unsafe characters from ISO-8601 timestamps.
            // E.g. "2026-04-08T14:32:00.0000000Z" → "2026-04-08T14-32-00-0000000Z"
            string safeId = (matchTimestamp ?? "unknown")
                .Replace(':', '-')
                .Replace('.', '-')
                .Replace('+', '-');

            return Path.Combine(
                Application.persistentDataPath,
                ReplayFilePrefix + safeId + ReplayFileSuffix);
        }
    }
}

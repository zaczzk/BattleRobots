using System;
using System.Collections.Generic;

namespace BattleRobots.Core
{
    /// <summary>
    /// Serializable snapshot of audio volume settings for persistence.
    /// Stored inside <see cref="SaveData.settingsSnapshot"/> and round-trips cleanly
    /// through JsonUtility / XOR SaveSystem.
    ///
    /// Default values are all 1.0 (full volume) so that saves that predate this field
    /// load at full volume rather than silent.
    /// </summary>
    [Serializable]
    public sealed class SettingsSnapshot
    {
        /// <summary>Master volume multiplier in [0, 1]. Default 1.0.</summary>
        public float masterVolume = 1f;

        /// <summary>SFX channel volume multiplier in [0, 1]. Default 1.0.</summary>
        public float sfxVolume = 1f;

        /// <summary>Music channel volume multiplier in [0, 1]. Default 1.0.</summary>
        public float musicVolume = 1f;
    }


    /// <summary>
    /// Serializable data class written to disk at the end of every match.
    /// Intentionally a plain POCO — no Unity types — so it round-trips cleanly
    /// through JsonUtility / XOR SaveSystem.
    /// </summary>
    [Serializable]
    public sealed class MatchRecord
    {
        /// <summary>UTC ISO-8601 timestamp of match end, e.g. "2026-04-05T14:32:00Z".</summary>
        public string timestamp;

        /// <summary>Zero-based arena index the match was played in.</summary>
        public int arenaIndex;

        /// <summary>True if the local player won.</summary>
        public bool playerWon;

        /// <summary>Match duration in seconds.</summary>
        public float durationSeconds;

        /// <summary>Damage dealt by the player's robot during the match.</summary>
        public float damageDone;

        /// <summary>Damage received by the player's robot.</summary>
        public float damageTaken;

        /// <summary>Currency earned this match (before shop spending).</summary>
        public int currencyEarned;

        /// <summary>Player wallet balance at the time the record was written.</summary>
        public int walletSnapshot;

        /// <summary>Ordered list of part IDs the robot had equipped.</summary>
        public List<string> equippedPartIds = new List<string>();
    }

    /// <summary>
    /// Top-level save file container. Holds the running wallet balance,
    /// the full match history, and the set of part IDs the player owns.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>Current persisted wallet balance.</summary>
        public int walletBalance;

        /// <summary>All match records in chronological order.</summary>
        public List<MatchRecord> matchHistory = new List<MatchRecord>();

        /// <summary>
        /// Part IDs unlocked via the shop. Populated by ShopManager after each
        /// purchase; rehydrated into <see cref="PlayerInventory"/> by
        /// <see cref="GameBootstrapper"/> at startup.
        /// Initialised to an empty list so JsonUtility round-trips without null
        /// even when deserialising saves that predate this field.
        /// </summary>
        public List<string> unlockedPartIds = new List<string>();

        /// <summary>
        /// Audio volume preferences written by <see cref="BattleRobots.UI.SettingsController"/>
        /// when the settings panel closes; restored by <see cref="GameBootstrapper"/> on startup.
        /// Initialised to a new instance (all volumes 1.0) so saves predating this field
        /// load at full volume.
        /// </summary>
        public SettingsSnapshot settingsSnapshot = new SettingsSnapshot();

        /// <summary>
        /// Part IDs that form the player's saved loadout (the build they want to use next match).
        /// Written by <see cref="PlayerLoadout"/> via the caller's Load→mutate→Save pattern.
        /// Initialised to an empty list so saves predating this field load with an empty loadout.
        /// </summary>
        public List<string> loadoutPartIds = new List<string>();
    }
}

using System;
using System.Collections.Generic;

namespace BattleRobots.Core
{
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

    // ── Loadout persistence ──────────────────────────────────────────────────

    /// <summary>
    /// Single slot→part mapping entry; serialized inside <see cref="RobotLoadoutData"/>.
    /// </summary>
    [Serializable]
    public sealed class LoadoutEntry
    {
        /// <summary>Slot identifier on the robot chassis (e.g. "slot_weapon_0").</summary>
        public string slotId;

        /// <summary>Part identifier of the equipped part (e.g. "weapon_sawblade").</summary>
        public string partId;
    }

    /// <summary>
    /// Persisted robot loadout — maps slot IDs to the currently equipped part IDs.
    /// Serialized inside <see cref="SaveData.robotLoadout"/> as a flat list because
    /// <c>JsonUtility</c> does not support <c>Dictionary</c>.
    /// </summary>
    [Serializable]
    public sealed class RobotLoadoutData
    {
        /// <summary>All slot→part assignments for the current robot configuration.</summary>
        public List<LoadoutEntry> entries = new List<LoadoutEntry>();
    }

    // ── Settings ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Persisted player settings — serialized inside SaveData so a single
    /// XOR-encrypted file holds both economy state and preferences.
    /// </summary>
    [Serializable]
    public sealed class SettingsData
    {
        /// <summary>Master audio volume [0, 1]. Applied to AudioListener.volume.</summary>
        public float masterVolume = 1f;

        /// <summary>SFX bus volume [0, 1]. Applied to SFXPlayer.SetMasterVolume.</summary>
        public float sfxVolume = 1f;

        /// <summary>When true, vertical camera/aim axis is inverted.</summary>
        public bool invertControls = false;
    }

    // ── Key bindings ──────────────────────────────────────────────────────────

    /// <summary>
    /// Single action → key mapping entry.
    /// <c>keyCode</c> is stored as <c>int</c> (the underlying value of <see cref="UnityEngine.KeyCode"/>)
    /// because <c>JsonUtility</c> does not serialise enums by name — only by value.
    /// Cast back to <c>KeyCode</c> at runtime.
    /// </summary>
    [Serializable]
    public sealed class KeyBindingEntry
    {
        /// <summary>Logical action name, e.g. "Forward", "Back", "Left", "Right", "Fire".</summary>
        public string actionName;

        /// <summary>The assigned key stored as int. Cast to <c>KeyCode</c> when reading.</summary>
        public int keyCode;
    }

    /// <summary>
    /// Flat list of <see cref="KeyBindingEntry"/> objects that persists all custom
    /// key bindings in a <c>JsonUtility</c>-compatible format.
    /// </summary>
    [Serializable]
    public sealed class KeyBindingsData
    {
        /// <summary>All action→key pairs. Order is not significant.</summary>
        public List<KeyBindingEntry> entries = new List<KeyBindingEntry>();
    }

    /// <summary>
    /// Top-level save file container. Holds the running wallet balance,
    /// full match history, and player settings.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>Current persisted wallet balance.</summary>
        public int walletBalance;

        /// <summary>All match records in chronological order.</summary>
        public List<MatchRecord> matchHistory = new List<MatchRecord>();

        /// <summary>Persisted player settings (volume, input preferences).</summary>
        public SettingsData settings = new SettingsData();

        /// <summary>
        /// Currently equipped parts, keyed by slot ID.
        /// Populated by <see cref="RobotLoadoutSO.BuildData"/> and consumed by
        /// <see cref="RobotLoadoutSO.LoadFromData"/>.
        /// </summary>
        public RobotLoadoutData robotLoadout = new RobotLoadoutData();

        /// <summary>
        /// Persisted custom key bindings.
        /// Populated by <see cref="SettingsSO.BuildKeyBindings"/> and consumed by
        /// <see cref="SettingsSO.LoadKeyBindings"/>.
        /// </summary>
        public KeyBindingsData keyBindings = new KeyBindingsData();
    }
}

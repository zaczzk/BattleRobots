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

        /// <summary>
        /// Display name of the <see cref="DifficultySO"/> preset active during this match.
        /// Empty string when no difficulty SO was assigned.
        /// </summary>
        public string difficultyName;
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

    // ── Player profile ────────────────────────────────────────────────────────

    /// <summary>
    /// Persisted player profile data — display name, avatar selection, and
    /// accumulated career statistics.
    /// Serialized inside <see cref="SaveData.playerProfile"/>.
    /// </summary>
    [Serializable]
    public sealed class PlayerProfileData
    {
        /// <summary>Player-chosen display name shown in the room list and lobby.</summary>
        public string displayName = "Player";

        /// <summary>Zero-based index into the game's avatar sprite array.</summary>
        public int avatarIndex = 0;

        /// <summary>Career total wins across all matches.</summary>
        public int careerWins;

        /// <summary>Career total losses.</summary>
        public int careerLosses;

        /// <summary>Career total currency earned.</summary>
        public int careerEarnings;

        /// <summary>Career total damage dealt.</summary>
        public float careerDamageDone;
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

        /// <summary>
        /// Room codes the player has starred as favourites.
        /// Populated by <see cref="FavouriteRoomsSO.BuildData"/> and consumed by
        /// <see cref="FavouriteRoomsSO.LoadFromData"/>.
        /// </summary>
        public List<string> favouriteRoomCodes = new List<string>();

        /// <summary>
        /// Most-recently-joined room codes, newest-first, capped at 10 entries.
        /// Populated by <see cref="RecentRoomsSO.BuildData"/> and consumed by
        /// <see cref="RecentRoomsSO.LoadFromData"/>.
        /// </summary>
        public List<string> recentRoomCodes = new List<string>();

        /// <summary>
        /// Persisted player profile (display name, avatar, career stats).
        /// Populated by <see cref="PlayerProfileSO.BuildData"/> and consumed by
        /// <see cref="PlayerProfileSO.LoadFromData"/>.
        /// </summary>
        public PlayerProfileData playerProfile = new PlayerProfileData();

        /// <summary>
        /// Persisted friend and block lists.
        /// Populated by <see cref="FriendListSO.BuildData"/> and consumed by
        /// <see cref="FriendListSO.LoadFromData"/>.
        /// </summary>
        public FriendListData friendList = new FriendListData();

        /// <summary>
        /// Persisted set of unlocked achievement IDs.
        /// Populated by <see cref="AchievementProgressSO.BuildData"/> and consumed by
        /// <see cref="AchievementProgressSO.LoadFromData"/>.
        /// </summary>
        public AchievementData achievements = new AchievementData();

        /// <summary>
        /// Persisted daily-challenge state (current UTC date, progress, reward-claimed flag).
        /// Populated by <see cref="DailyChallengeProgressSO.BuildData"/> and consumed by
        /// <see cref="DailyChallengeProgressSO.RefreshForToday"/>.
        /// </summary>
        public DailyChallengeData dailyChallenge = new DailyChallengeData();

        /// <summary>
        /// Persisted seasonal-event state (season ID, cumulative score, claimed tier indices).
        /// Populated by <see cref="SeasonalEventProgressSO.BuildData"/> and consumed by
        /// <see cref="SeasonalEventProgressSO.LoadFromData"/>.
        /// </summary>
        public SeasonalEventData seasonalEvent = new SeasonalEventData();
    }

    // ── Seasonal event persistence ────────────────────────────────────────────

    /// <summary>
    /// Persisted state for the player's current seasonal-event progress.
    /// Season-ID mismatch in <see cref="SeasonalEventProgressSO.LoadFromData"/> triggers
    /// a full reset so previous-season data never carries forward.
    /// Serialized inside <see cref="SaveData.seasonalEvent"/>.
    /// </summary>
    [Serializable]
    public sealed class SeasonalEventData
    {
        /// <summary>
        /// <see cref="SeasonalEventDefinitionSO.EventId"/> of the season for which this
        /// data was recorded. Empty string means no season has been started.
        /// </summary>
        public string seasonId = string.Empty;

        /// <summary>Cumulative seasonal score accumulated across all matches this season.</summary>
        public int cumulativeScore;

        /// <summary>
        /// Zero-based indices of tiers the player has already claimed rewards for.
        /// Flat list because JsonUtility does not support HashSet.
        /// </summary>
        public List<int> claimedTierIndices = new List<int>();
    }

    // ── Friend / block list ───────────────────────────────────────────────────

    /// <summary>
    /// Persisted friend and block list data.
    /// Two separate lists allow a player to be blocked without ever being friended.
    /// Invariant: a name can appear in at most one list (friends XOR blocked).
    /// </summary>
    [Serializable]
    public sealed class FriendListData
    {
        /// <summary>Display names of players the local player has added as friends.</summary>
        public List<string> friendNames = new List<string>();

        /// <summary>Display names of players the local player has blocked.</summary>
        public List<string> blockedNames = new List<string>();
    }

    // ── Achievement persistence ────────────────────────────────────────────────

    /// <summary>
    /// Persisted set of unlocked achievement IDs.
    /// Serialized inside <see cref="SaveData.achievements"/>.
    /// Duplicate or empty IDs are silently dropped during
    /// <see cref="AchievementProgressSO.LoadFromData"/> hydration.
    /// </summary>
    [Serializable]
    public sealed class AchievementData
    {
        /// <summary>IDs of every achievement the player has unlocked, in unlock order.</summary>
        public List<string> unlockedIds = new List<string>();
    }

    // ── Daily challenge persistence ────────────────────────────────────────────

    /// <summary>
    /// Persisted state for the current day's challenge.
    /// Serialized inside <see cref="SaveData.dailyChallenge"/>.
    ///
    /// Fields are intentionally flat (no nested lists) for JsonUtility compatibility.
    /// </summary>
    [Serializable]
    public sealed class DailyChallengeData
    {
        /// <summary>
        /// UTC date string ("YYYY-MM-DD") for which this data was last written.
        /// An empty string signals that no challenge has been started yet.
        /// </summary>
        public string lastDateUtc = string.Empty;

        /// <summary>
        /// <see cref="DailyChallengeDefinitionSO.ChallengeId"/> of the active challenge
        /// for <see cref="lastDateUtc"/>. Empty means no challenge was selected.
        /// </summary>
        public string challengeId = string.Empty;

        /// <summary>
        /// Raw accumulated progress units toward the challenge target.
        /// Reset to 0 when a new day is detected.
        /// </summary>
        public float progress = 0f;

        /// <summary>
        /// True once the player has claimed the completion reward for this challenge.
        /// Prevents double-crediting across sessions on the same UTC day.
        /// </summary>
        public bool rewardClaimed = false;
    }

    // ── Replay export ─────────────────────────────────────────────────────────

    /// <summary>
    /// Single serializable replay frame — a subset of <see cref="MatchStateSnapshot"/>
    /// fields flattened to avoid Unity-type dependencies in this POCO file.
    ///
    /// <c>Vector3</c> is broken into component floats so the record is serializable
    /// without a <c>using UnityEngine</c> import and survives non-Unity serialization
    /// paths (e.g. unit tests that use System.Text.Json instead of JsonUtility).
    /// </summary>
    [Serializable]
    public sealed class ReplayFrame
    {
        /// <summary>Elapsed match time in seconds at the moment this frame was captured.</summary>
        public float elapsedTime;

        /// <summary>Player 1 hit-points.</summary>
        public float p1Hp;

        /// <summary>Player 2 hit-points.</summary>
        public float p2Hp;

        /// <summary>Player 1 world-space X position.</summary>
        public float p1X;
        /// <summary>Player 1 world-space Y position.</summary>
        public float p1Y;
        /// <summary>Player 1 world-space Z position.</summary>
        public float p1Z;

        /// <summary>Player 2 world-space X position.</summary>
        public float p2X;
        /// <summary>Player 2 world-space Y position.</summary>
        public float p2Y;
        /// <summary>Player 2 world-space Z position.</summary>
        public float p2Z;
    }

    /// <summary>
    /// Full replay export for a single match.
    /// Stored in a separate XOR-encrypted file (not inside the main save file)
    /// to keep save-file size bounded regardless of match length.
    ///
    /// Lifecycle:
    ///   1. After a match ends, call <see cref="ReplayData.FromReplaySO"/> to build this object.
    ///   2. Pass it to <see cref="SaveSystem.SaveReplay"/> to persist it.
    ///   3. Use <see cref="SaveSystem.LoadReplay"/> to retrieve it later for the replay viewer.
    ///
    /// File naming: <c>replay_{sanitizedTimestamp}.sav</c> — the timestamp
    /// matches <see cref="MatchRecord.timestamp"/> so replays are linkable to records.
    /// </summary>
    [Serializable]
    public sealed class ReplayData
    {
        /// <summary>
        /// UTC ISO-8601 timestamp copied from the corresponding <see cref="MatchRecord.timestamp"/>.
        /// Used as the unique replay identifier and to derive the save-file name.
        /// </summary>
        public string matchTimestamp;

        /// <summary>Zero-based arena index the match was played in.</summary>
        public int arenaIndex;

        /// <summary>
        /// Ordered replay frames, oldest first.
        /// Populated from <see cref="ReplaySO"/> snapshot data.
        /// </summary>
        public List<ReplayFrame> frames = new List<ReplayFrame>();

        /// <summary>
        /// Builds a <see cref="ReplayData"/> from a <see cref="ReplaySO"/> ring buffer
        /// and the associated <see cref="MatchRecord"/>.
        ///
        /// Copies all snapshots out of the ring buffer in oldest-first order.
        /// The <paramref name="replay"/> must not be null; it may be empty (returns
        /// a ReplayData with no frames).
        /// </summary>
        public static ReplayData FromReplaySO(ReplaySO replay, MatchRecord record)
        {
            if (replay == null)
                throw new ArgumentNullException(nameof(replay));

            var data = new ReplayData
            {
                matchTimestamp = record?.timestamp ?? string.Empty,
                arenaIndex     = record?.arenaIndex ?? 0,
            };

            int count = replay.SnapshotCount;
            data.frames.Capacity = count;

            for (int i = 0; i < count; i++)
            {
                MatchStateSnapshot snap = replay.GetSnapshot(i);
                data.frames.Add(new ReplayFrame
                {
                    elapsedTime = snap.elapsedTime,
                    p1Hp        = snap.p1Hp,
                    p2Hp        = snap.p2Hp,
                    p1X         = snap.p1Pos.x,
                    p1Y         = snap.p1Pos.y,
                    p1Z         = snap.p1Pos.z,
                    p2X         = snap.p2Pos.x,
                    p2Y         = snap.p2Pos.y,
                    p2Z         = snap.p2Pos.z,
                });
            }

            return data;
        }
    }
}

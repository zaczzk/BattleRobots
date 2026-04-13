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

        /// <summary>
        /// Display name of the selected opponent profile (from <see cref="OpponentProfileSO.DisplayName"/>).
        /// Empty string when no opponent was selected via the pre-match lobby.
        /// Backwards-compatible: JsonUtility deserialises missing string fields as null;
        /// consumer code should treat null as empty.
        /// </summary>
        public string opponentName = "";

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
    /// A named snapshot of the player's equipped-part list, stored as a loadout preset.
    ///
    /// Created by <see cref="LoadoutPresetManagerSO.SavePreset"/> and serialised into
    /// <see cref="SaveData.savedLoadoutPresets"/> via the XOR SaveSystem.
    ///
    /// Immutable once created: the name and part-ID list never change after construction.
    /// </summary>
    [Serializable]
    public sealed class SavedLoadoutPreset
    {
        /// <summary>Display name chosen by the player (e.g. "Speed Build"). Never null.</summary>
        public string name = "";

        /// <summary>
        /// Snapshot of the equipped part IDs at the time the preset was saved.
        /// May be empty (player saved an empty loadout). Never null.
        /// </summary>
        public List<string> partIds = new List<string>();
    }

    /// <summary>
    /// Serializable snapshot of a single part's HP ratio for persistence across sessions.
    /// Stored inside <see cref="SaveData.savedPartConditions"/> and round-trips through
    /// JsonUtility / XOR SaveSystem.
    ///
    /// <see cref="hpRatio"/> is stored as a ratio in [0, 1] rather than raw HP so that
    /// changes to a <see cref="PartConditionSO"/> MaxHP inspector value do not produce
    /// out-of-range HP after a save reload.
    /// </summary>
    [Serializable]
    public sealed class PartConditionSnapshot
    {
        /// <summary>
        /// Part ID that identifies which <see cref="PartConditionSO"/> this snapshot belongs to.
        /// Matched against registry entries in <see cref="PartConditionRegistry"/>.
        /// </summary>
        public string partId = "";

        /// <summary>HP ratio in [0, 1] at the time the snapshot was taken. 1.0 = full health.</summary>
        public float hpRatio = 1f;
    }

    /// <summary>
    /// A single entry in the local match leaderboard, capturing the score and key
    /// context of the match that produced it.
    ///
    /// Stored inside <see cref="SaveData.leaderboardEntries"/> and round-trips cleanly
    /// through JsonUtility / XOR SaveSystem.
    ///
    /// Default values are all-zero / empty so that saves predating this type deserialise
    /// safely without corrupting the leaderboard list.
    /// </summary>
    [Serializable]
    public sealed class LeaderboardEntry
    {
        /// <summary>Numeric score computed by <see cref="MatchScoreCalculator"/>. Always ≥ 0.</summary>
        public int score;

        /// <summary>True when the player won the match that produced this score.</summary>
        public bool playerWon;

        /// <summary>
        /// Display name of the selected opponent profile.
        /// Empty string when no opponent was selected via the pre-match lobby.
        /// </summary>
        public string opponentName = "";

        /// <summary>Zero-based arena index the match was played in.</summary>
        public int arenaIndex;

        /// <summary>Match duration in seconds.</summary>
        public float durationSeconds;

        /// <summary>UTC ISO-8601 timestamp of match end (e.g. "2026-04-12T14:32:00Z").</summary>
        public string timestamp = "";
    }

    /// <summary>
    /// Serializable snapshot of the player's all-time single-match career bests for
    /// persistence across sessions.
    /// Stored inside <see cref="SaveData.careerHighlights"/> and round-trips cleanly
    /// through JsonUtility / XOR SaveSystem.
    ///
    /// Default values are all-zero meaning "no match played yet".
    /// </summary>
    [Serializable]
    public sealed class CareerHighlightsSnapshot
    {
        /// <summary>Highest damage dealt in a single match ever played. 0 = none yet.</summary>
        public float bestSingleMatchDamage;

        /// <summary>
        /// Fastest winning match duration in seconds.
        /// 0 = player has never won a match.
        /// </summary>
        public float fastestWinSeconds;

        /// <summary>Highest currency earned in a single match. 0 = none yet.</summary>
        public int bestSingleMatchCurrency;

        /// <summary>Longest match duration ever played in seconds. 0 = no match played.</summary>
        public float longestMatchSeconds;
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

        /// <summary>
        /// Part IDs that have been upgraded at least once.
        /// Parallel to <see cref="upgradePartTierValues"/> — indices must match.
        /// Initialised to an empty list so saves predating this field load with no upgrades.
        /// </summary>
        public List<string> upgradePartIds = new List<string>();

        /// <summary>
        /// Upgrade tier values for each upgraded part.
        /// Parallel to <see cref="upgradePartIds"/> — indices must match.
        /// Initialised to an empty list so saves predating this field load with no upgrades.
        /// </summary>
        public List<int> upgradePartTierValues = new List<int>();

        /// <summary>
        /// Number of consecutive wins at the time this save was written.
        /// Initialised to 0 so saves predating this field start with no streak.
        /// </summary>
        public int currentWinStreak;

        /// <summary>
        /// All-time highest consecutive-win streak.
        /// Initialised to 0 so saves predating this field start with no best streak.
        /// </summary>
        public int bestWinStreak;

        /// <summary>
        /// Total cumulative XP earned by the player.
        /// Initialised to 0 so saves predating this field start with no XP.
        /// <see cref="PlayerProgressionSO.LoadSnapshot"/> treats level 0 as level 1.
        /// </summary>
        public int playerTotalXP;

        /// <summary>
        /// Player's current level (1-based).
        /// Initialised to 0; <see cref="PlayerProgressionSO.LoadSnapshot"/> clamps
        /// any value &lt; 1 to level 1 — safe for saves predating this field.
        /// </summary>
        public int playerLevel;

        /// <summary>
        /// Total matches played (win + loss) used by the achievement system.
        /// Initialised to 0; backwards-compatible with saves predating this field.
        /// </summary>
        public int totalMatchesPlayed;

        /// <summary>
        /// Total matches won used by the achievement system.
        /// Initialised to 0; backwards-compatible with saves predating this field.
        /// </summary>
        public int totalMatchesWon;

        /// <summary>
        /// Achievement IDs that have been unlocked by the player.
        /// Initialised to an empty list so saves predating this field start
        /// with no achievements unlocked.
        /// </summary>
        public List<string> unlockedAchievementIds = new List<string>();

        // ── Career statistics (T071) ───────────────────────────────────────────

        /// <summary>
        /// Cumulative damage dealt to enemies across all matches ever played.
        /// Written by <see cref="PlayerCareerStatsSO.PatchSaveData"/> in EndMatch.
        /// Default 0 — backwards-compatible with saves predating this field.
        /// </summary>
        public float careerDamageDealt;

        /// <summary>
        /// Cumulative damage received by the player across all matches ever played.
        /// Written by <see cref="PlayerCareerStatsSO.PatchSaveData"/> in EndMatch.
        /// Default 0 — backwards-compatible with saves predating this field.
        /// </summary>
        public float careerDamageTaken;

        /// <summary>
        /// Cumulative currency earned (before shop spending) across all matches.
        /// Written by <see cref="PlayerCareerStatsSO.PatchSaveData"/> in EndMatch.
        /// Default 0 — backwards-compatible with saves predating this field.
        /// </summary>
        public int careerCurrencyEarned;

        /// <summary>
        /// Cumulative match time in seconds across all matches ever played.
        /// Written by <see cref="PlayerCareerStatsSO.PatchSaveData"/> in EndMatch.
        /// Default 0 — backwards-compatible with saves predating this field.
        /// </summary>
        public float careerPlaytimeSeconds;

        // ── Daily Challenge (T084) ─────────────────────────────────────────────

        /// <summary>
        /// UTC date (yyyy-MM-dd) of the last daily-challenge refresh.
        /// Empty string on saves predating this field — treated as "never refreshed"
        /// so a new challenge is picked on the next session.
        /// </summary>
        public string dailyChallengeDate = "";

        /// <summary>
        /// Index into <see cref="BattleRobots.Core.DailyChallengeConfig.ChallengePool"/>
        /// of the challenge selected for <see cref="dailyChallengeDate"/>.
        /// -1 when no challenge has been selected.
        /// JsonUtility deserialises missing int fields as 0; the date mismatch logic in
        /// <see cref="BattleRobots.Core.DailyChallengeSO.RefreshIfNeeded"/> handles this
        /// safely — a 0 index from an old save is harmless because the date will not
        /// match today and a fresh selection will be made.
        /// </summary>
        public int dailyChallengeIndex = -1;

        /// <summary>
        /// Whether the daily challenge for <see cref="dailyChallengeDate"/> has been
        /// completed.  False by default (backwards-compatible).
        /// </summary>
        public bool dailyChallengeCompleted;

        // ── Personal Best Score (T094) ─────────────────────────────────────────

        /// <summary>
        /// The player's all-time highest match score as computed by
        /// <see cref="MatchScoreCalculator"/> and tracked by <see cref="PersonalBestSO"/>.
        /// Written by <see cref="MatchManager"/> in EndMatch via
        /// <see cref="PersonalBestSO.BestScore"/> after each <see cref="PersonalBestSO.Submit"/>.
        /// Default 0 — backwards-compatible with saves predating this field.
        /// </summary>
        public int personalBestScore;

        // ── Loadout Presets (T103) ─────────────────────────────────────────────

        /// <summary>
        /// Named loadout presets saved by the player in the pre-match lobby.
        /// Each entry stores a display name and a snapshot of equipped part IDs.
        /// Written by <see cref="LoadoutPresetManagerSO"/> via the load→mutate→Save
        /// round-trip in <see cref="BattleRobots.UI.LoadoutPresetController"/>.
        /// Initialised to an empty list so saves predating this field load with no presets.
        /// </summary>
        public List<SavedLoadoutPreset> savedLoadoutPresets = new List<SavedLoadoutPreset>();

        // ── Part Durability (T107) ─────────────────────────────────────────────

        /// <summary>
        /// Per-part HP ratios persisted at the end of each session.
        /// Restored by <see cref="GameBootstrapper"/> on startup via
        /// <see cref="PartConditionRegistry.LoadSnapshot"/>, overriding the
        /// default full-HP state that <see cref="PartConditionSO.OnEnable"/> applies.
        /// Initialised to an empty list so saves predating this field start with
        /// all parts at full HP (backwards-compatible).
        /// </summary>
        public List<PartConditionSnapshot> savedPartConditions = new List<PartConditionSnapshot>();

        // ── Leaderboard (T116) ────────────────────────────────────────────────

        /// <summary>
        /// Top-N match scores for the local leaderboard panel.
        /// Sorted descending by score; capacity is capped at
        /// <see cref="MatchLeaderboardSO.MaxEntries"/> at write time.
        /// Rehydrated into <see cref="MatchLeaderboardSO"/> by <see cref="GameBootstrapper"/>
        /// on startup via <see cref="MatchLeaderboardSO.LoadSnapshot"/>.
        /// Initialised to an empty list so saves predating this field load with no entries.
        /// </summary>
        public List<LeaderboardEntry> leaderboardEntries = new List<LeaderboardEntry>();

        // ── Score History (T117) ───────────────────────────────────────────────

        /// <summary>
        /// Chronological rolling window of the last N match scores (oldest first).
        /// Rehydrated into <see cref="ScoreHistorySO"/> by <see cref="GameBootstrapper"/>
        /// on startup via <see cref="ScoreHistorySO.LoadSnapshot"/>.
        /// Initialised to an empty list so saves predating this field load with no history.
        /// </summary>
        public List<int> scoreHistoryScores = new List<int>();

        // ── Career Highlights (T120) ──────────────────────────────────────────

        /// <summary>
        /// Per-category single-match all-time bests (best damage, fastest win, etc.).
        /// Rehydrated into <see cref="CareerHighlightsSO"/> by <see cref="GameBootstrapper"/>
        /// on startup via <see cref="CareerHighlightsSO.LoadSnapshot"/>.
        /// Default is a fresh instance (all zeros) — backwards-compatible with saves
        /// predating this field.
        /// </summary>
        public CareerHighlightsSnapshot careerHighlights = new CareerHighlightsSnapshot();

        // ── Prestige (T123) ───────────────────────────────────────────────────

        /// <summary>
        /// Number of times the player has prestiged (voluntarily reset to Level 1 after
        /// reaching max level in exchange for a permanent rank badge).
        /// Rehydrated into <see cref="PrestigeSystemSO"/> by <see cref="GameBootstrapper"/>
        /// on startup via <see cref="PrestigeSystemSO.LoadSnapshot"/>.
        /// Default 0 — backwards-compatible with saves predating this field.
        /// </summary>
        public int prestigeCount;

        // ── Tutorial Progress (T126) ──────────────────────────────────────────

        /// <summary>
        /// Whether the player has completed or explicitly skipped the tutorial.
        /// Rehydrated into <see cref="TutorialProgressSO"/> by <see cref="GameBootstrapper"/>
        /// on startup via <see cref="TutorialProgressSO.LoadSnapshot"/>.
        /// Default false — new players see the tutorial; backwards-compatible with saves
        /// predating this field (existing players also see it once, then it persists as true).
        /// </summary>
        public bool tutorialComplete;

        /// <summary>
        /// Step IDs that the player has individually acknowledged during the tutorial.
        /// Rehydrated into <see cref="TutorialProgressSO"/> by <see cref="GameBootstrapper"/>
        /// on startup via <see cref="TutorialProgressSO.LoadSnapshot"/>.
        /// Initialised to an empty list so saves predating this field load with no completed steps.
        /// </summary>
        public List<string> completedTutorialStepIds = new List<string>();
    }
}

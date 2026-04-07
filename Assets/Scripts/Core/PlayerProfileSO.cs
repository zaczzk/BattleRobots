using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject holding the player's persistent identity and
    /// accumulated career statistics.
    ///
    /// All mutation goes through the designated mutators so event channels fire
    /// consistently and the SO-immutability rule is honoured.
    ///
    /// ── Persistence ───────────────────────────────────────────────────────────
    ///   <see cref="BuildData"/> snapshots state into a <see cref="PlayerProfileData"/>
    ///   POCO for XOR-SaveSystem persistence; <see cref="LoadFromData"/> restores it.
    ///   <see cref="GameBootstrapper"/> calls both on startup and match-save respectively.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   • Assign this SO to <see cref="GameBootstrapper._playerProfile"/>.
    ///   • Add a VoidGameEventListener to the ProfileUI panel:
    ///       Event = <see cref="_onProfileChanged"/>, Response = ProfileUI.HandleProfileChanged()
    ///   • Add a StringGameEventListener for <see cref="_onNameChanged"/> → label refresh.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core only — no Physics/UI namespace references.
    ///   - No heap allocations in mutators (string assignments are infrequent).
    ///   - <see cref="UpdateFromMatchRecord"/> is called once per match end (cold path).
    ///
    /// Create via:  Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ PlayerProfileSO
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Economy/PlayerProfileSO", order = 1)]
    public sealed class PlayerProfileSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Defaults")]
        [Tooltip("Display name used when no save data exists (first launch).")]
        [SerializeField] private string _defaultDisplayName = "Player";

        [Tooltip("Avatar index used on first launch.")]
        [SerializeField, Min(0)] private int _defaultAvatarIndex = 0;

        [Header("Event Channels — Out")]
        [Tooltip("Raised whenever any profile field changes (name, avatar, or stats).")]
        [SerializeField] private VoidGameEvent _onProfileChanged;

        [Tooltip("Raised when the display name changes. Payload = new name.")]
        [SerializeField] private StringGameEvent _onNameChanged;

        // ── Runtime state ─────────────────────────────────────────────────────

        /// <summary>Player-chosen display name shown in the lobby and room list.</summary>
        public string DisplayName   { get; private set; } = "Player";

        /// <summary>Zero-based index into the game's avatar sprite array.</summary>
        public int    AvatarIndex   { get; private set; } = 0;

        // ── Career stats (read-only) ──────────────────────────────────────────

        /// <summary>Total wins across all sessions.</summary>
        public int    CareerWins        { get; private set; }

        /// <summary>Total losses across all sessions.</summary>
        public int    CareerLosses      { get; private set; }

        /// <summary>Total currency earned across all sessions.</summary>
        public int    CareerEarnings    { get; private set; }

        /// <summary>Total damage dealt across all sessions.</summary>
        public float  CareerDamageDone  { get; private set; }

        /// <summary>
        /// Total matches played (wins + losses).
        /// Computed on demand — no allocation.
        /// </summary>
        public int    CareerMatches     => CareerWins + CareerLosses;

        /// <summary>
        /// Win rate as a value in [0, 1].
        /// Returns 0 when no matches have been played.
        /// </summary>
        public float  WinRate           => CareerMatches > 0
                                            ? (float)CareerWins / CareerMatches
                                            : 0f;

        // ── Designated mutators ───────────────────────────────────────────────

        /// <summary>
        /// Sets the player's display name.
        /// Strips leading/trailing whitespace; ignores empty or whitespace-only input.
        /// Fires <see cref="_onNameChanged"/> and <see cref="_onProfileChanged"/>.
        /// </summary>
        public void SetDisplayName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            string trimmed = name.Trim();
            if (trimmed == DisplayName) return;

            DisplayName = trimmed;
            _onNameChanged?.Raise(DisplayName);
            _onProfileChanged?.Raise();
        }

        /// <summary>
        /// Sets the avatar selection index.
        /// Clamps to <c>≥ 0</c>. Fires <see cref="_onProfileChanged"/>.
        /// </summary>
        public void SetAvatarIndex(int index)
        {
            int clamped = index < 0 ? 0 : index;
            if (clamped == AvatarIndex) return;

            AvatarIndex = clamped;
            _onProfileChanged?.Raise();
        }

        /// <summary>
        /// Accumulates career stats from a completed match record.
        /// Safe to call with a null record (no-op).
        /// Fires <see cref="_onProfileChanged"/> after update.
        /// </summary>
        public void UpdateFromMatchRecord(MatchRecord record)
        {
            if (record == null) return;

            if (record.playerWon) CareerWins++;
            else                  CareerLosses++;

            CareerEarnings   += record.currencyEarned;
            CareerDamageDone += record.damageDone;

            _onProfileChanged?.Raise();
        }

        // ── Persistence ───────────────────────────────────────────────────────

        /// <summary>
        /// Restores runtime state from a persisted <see cref="PlayerProfileData"/> snapshot.
        /// Called by <see cref="GameBootstrapper"/> on startup.
        /// Does not fire event channels — caller is responsible for UI refresh.
        /// </summary>
        public void LoadFromData(PlayerProfileData data)
        {
            if (data == null)
            {
                ResetToDefaults();
                return;
            }

            DisplayName      = string.IsNullOrWhiteSpace(data.displayName)
                                   ? _defaultDisplayName
                                   : data.displayName.Trim();
            AvatarIndex      = data.avatarIndex < 0 ? 0 : data.avatarIndex;
            CareerWins       = data.careerWins    < 0 ? 0 : data.careerWins;
            CareerLosses     = data.careerLosses  < 0 ? 0 : data.careerLosses;
            CareerEarnings   = data.careerEarnings < 0 ? 0 : data.careerEarnings;
            CareerDamageDone = data.careerDamageDone < 0f ? 0f : data.careerDamageDone;
        }

        /// <summary>
        /// Snapshots runtime state into a <see cref="PlayerProfileData"/> POCO
        /// suitable for XOR-SaveSystem persistence.
        /// Called by <see cref="GameBootstrapper.RecordMatchAndSave"/>.
        /// </summary>
        public PlayerProfileData BuildData()
        {
            return new PlayerProfileData
            {
                displayName      = DisplayName,
                avatarIndex      = AvatarIndex,
                careerWins       = CareerWins,
                careerLosses     = CareerLosses,
                careerEarnings   = CareerEarnings,
                careerDamageDone = CareerDamageDone,
            };
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private void ResetToDefaults()
        {
            DisplayName      = _defaultDisplayName;
            AvatarIndex      = _defaultAvatarIndex;
            CareerWins       = 0;
            CareerLosses     = 0;
            CareerEarnings   = 0;
            CareerDamageDone = 0f;
        }

        // ── Editor guard ──────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_defaultDisplayName))
                Debug.LogWarning("[PlayerProfileSO] Default display name must not be empty.");
        }
#endif
    }
}

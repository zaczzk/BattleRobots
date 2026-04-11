using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that accumulates career-wide statistics not captured elsewhere:
    ///   • TotalDamageDealt  — sum of damageDone across all matches played this save
    ///   • TotalDamageTaken  — sum of damageTaken across all matches
    ///   • TotalCurrencyEarned — sum of currencyEarned across all matches
    ///   • TotalPlaytimeSeconds — sum of durationSeconds across all matches
    ///
    /// Per-match stats live in <see cref="MatchRecord"/>. Win/loss counts are owned
    /// by <see cref="PlayerAchievementsSO"/>. Streak is in <see cref="WinStreakSO"/>.
    /// This SO aggregates the financial and damage columns that have no other owner.
    ///
    /// ── Lifecycle ─────────────────────────────────────────────────────────────
    ///   • <see cref="GameBootstrapper"/> calls <see cref="LoadSnapshot"/> at startup.
    ///   • <see cref="MatchManager"/> calls <see cref="RecordMatch(MatchRecord)"/> in EndMatch.
    ///   • MatchManager also calls <see cref="PatchSaveData"/> before SaveSystem.Save().
    ///   • <see cref="Reset"/> is silent (no event) — intended for testing only.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime fields are NOT serialized (SO asset stays immutable on disk).
    ///   - Negative inputs are clamped to zero in all mutating methods.
    ///   - Zero and negative durations/damage are silently ignored in RecordMatch.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerCareerStats",
                     menuName  = "BattleRobots/Core/PlayerCareerStatsSO")]
    public sealed class PlayerCareerStatsSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel — Out")]
        [Tooltip("Raised after each RecordMatch() call so UI panels can refresh.")]
        [SerializeField] private VoidGameEvent _onStatsUpdated;

        // ── Runtime state (not serialized — reset each domain reload) ─────────

        private float _totalDamageDealt;
        private float _totalDamageTaken;
        private int   _totalCurrencyEarned;
        private float _totalPlaytimeSeconds;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Cumulative damage dealt to enemies across all matches (≥ 0).</summary>
        public float TotalDamageDealt     => _totalDamageDealt;

        /// <summary>Cumulative damage received by the player across all matches (≥ 0).</summary>
        public float TotalDamageTaken     => _totalDamageTaken;

        /// <summary>Cumulative currency earned (before spending) across all matches (≥ 0).</summary>
        public int   TotalCurrencyEarned  => _totalCurrencyEarned;

        /// <summary>Cumulative match time in seconds (≥ 0).</summary>
        public float TotalPlaytimeSeconds => _totalPlaytimeSeconds;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Adds one match's stats to the running totals and fires <c>_onStatsUpdated</c>.
        /// Negative values for any argument are clamped to zero before accumulation.
        /// </summary>
        public void RecordMatch(float damageDealt, float damageTaken,
                                int currencyEarned, float durationSeconds)
        {
            _totalDamageDealt     += damageDealt     > 0f ? damageDealt     : 0f;
            _totalDamageTaken     += damageTaken     > 0f ? damageTaken     : 0f;
            _totalCurrencyEarned  += currencyEarned  > 0  ? currencyEarned  : 0;
            _totalPlaytimeSeconds += durationSeconds > 0f ? durationSeconds : 0f;

            _onStatsUpdated?.Raise();
        }

        /// <summary>
        /// Convenience overload — reads fields directly from a <see cref="MatchRecord"/>.
        /// Null record is a no-op (no event fired).
        /// </summary>
        public void RecordMatch(MatchRecord record)
        {
            if (record == null) return;
            RecordMatch(record.damageDone, record.damageTaken,
                        record.currencyEarned, record.durationSeconds);
        }

        // ── Persistence helpers ───────────────────────────────────────────────

        /// <summary>
        /// Rehydrates runtime fields from persisted values. Silent (no event).
        /// Negative values are clamped to zero so old or corrupted saves load cleanly.
        /// </summary>
        public void LoadSnapshot(float damageDealt, float damageTaken,
                                 int currencyEarned, float playtimeSeconds)
        {
            _totalDamageDealt     = damageDealt     > 0f ? damageDealt     : 0f;
            _totalDamageTaken     = damageTaken     > 0f ? damageTaken     : 0f;
            _totalCurrencyEarned  = currencyEarned  > 0  ? currencyEarned  : 0;
            _totalPlaytimeSeconds = playtimeSeconds > 0f ? playtimeSeconds : 0f;
        }

        /// <summary>
        /// Writes the current accumulated totals into a <see cref="SaveData"/> object.
        /// Call this just before <see cref="SaveSystem.Save"/> in the match-end flow.
        /// Null <paramref name="save"/> is a no-op.
        /// </summary>
        public void PatchSaveData(SaveData save)
        {
            if (save == null) return;
            save.careerDamageDealt     = _totalDamageDealt;
            save.careerDamageTaken     = _totalDamageTaken;
            save.careerCurrencyEarned  = _totalCurrencyEarned;
            save.careerPlaytimeSeconds = _totalPlaytimeSeconds;
        }

        /// <summary>
        /// Clears all accumulated totals. Silent — does NOT fire <c>_onStatsUpdated</c>.
        /// Intended for testing; not called by game flow.
        /// </summary>
        public void Reset()
        {
            _totalDamageDealt     = 0f;
            _totalDamageTaken     = 0f;
            _totalCurrencyEarned  = 0;
            _totalPlaytimeSeconds = 0f;
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onStatsUpdated == null)
                UnityEditor.EditorUtility.SetDirty(this); // encourage wiring, non-fatal
        }
#endif
    }
}

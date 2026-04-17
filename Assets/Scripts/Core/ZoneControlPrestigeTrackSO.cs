using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks cumulative zone captures across all sessions and
    /// awards prestige titles as configurable thresholds are crossed.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <see cref="AddCaptures"/> accumulates the count and advances the
    ///   prestige tier whenever <c>_tierThresholds[i]</c> is reached, firing
    ///   <c>_onPrestigeGranted</c> per tier crossed.
    ///   <see cref="GetPrestigeTitle"/> returns the label for the current tier.
    ///   <see cref="Reset"/> silently clears all state; called from OnEnable.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — resets on play-mode entry.
    ///   - Zero heap allocation on all hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlPrestigeTrack.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlPrestigeTrack", order = 71)]
    public sealed class ZoneControlPrestigeTrackSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Prestige Tiers")]
        [Tooltip("Sorted capture-count thresholds — one per tier.")]
        [SerializeField] private int[]    _tierThresholds = { 100, 500, 1000, 5000 };

        [Tooltip("Title string for each tier (must match length of _tierThresholds).")]
        [SerializeField] private string[] _tierTitles     = { "Novice", "Expert", "Master", "Legend" };

        [Header("Event Channels (optional)")]
        [Tooltip("Raised once each time a new prestige tier is reached.")]
        [SerializeField] private VoidGameEvent _onPrestigeGranted;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _totalCaptures;
        private int _currentTierIndex;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Cumulative zone captures added since last Reset.</summary>
        public int TotalCaptures    => _totalCaptures;

        /// <summary>Index into _tierThresholds of the next tier to unlock (0 = none unlocked yet).</summary>
        public int CurrentTierIndex => _currentTierIndex;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="count"/> captures (negative values are ignored)
        /// and advances prestige tiers as thresholds are crossed.
        /// </summary>
        public void AddCaptures(int count)
        {
            if (count <= 0) return;
            _totalCaptures += count;
            EvaluateTiers();
        }

        /// <summary>
        /// Returns the prestige title for the highest tier reached.
        /// Returns an empty string when <c>_tierTitles</c> is empty.
        /// </summary>
        public string GetPrestigeTitle()
        {
            if (_tierTitles == null || _tierTitles.Length == 0)
                return string.Empty;

            int index = Mathf.Clamp(_currentTierIndex > 0 ? _currentTierIndex - 1 : 0,
                                    0, _tierTitles.Length - 1);
            return _currentTierIndex == 0 ? _tierTitles[0] : _tierTitles[index];
        }

        /// <summary>
        /// Clears all runtime state silently.  Called automatically by <c>OnEnable</c>.
        /// </summary>
        public void Reset()
        {
            _totalCaptures    = 0;
            _currentTierIndex = 0;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void EvaluateTiers()
        {
            if (_tierThresholds == null) return;
            while (_currentTierIndex < _tierThresholds.Length
                   && _totalCaptures >= _tierThresholds[_currentTierIndex])
            {
                _currentTierIndex++;
                _onPrestigeGranted?.Raise();
            }
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Tracks the player's consecutive win streak and all-time best streak.
    ///
    /// ── Mutators ──────────────────────────────────────────────────────────────
    ///   • <see cref="RecordWin"/>  — increments CurrentStreak; updates BestStreak
    ///     if the new value is a personal best; fires <c>_onStreakChanged</c>.
    ///   • <see cref="RecordLoss"/> — resets CurrentStreak to 0; fires
    ///     <c>_onStreakChanged</c>.  BestStreak is never reduced.
    ///   • <see cref="LoadSnapshot"/> — silent rehydration from SaveData (no event).
    ///   • <see cref="Reset"/>       — silent clear for fresh installs (no event).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO mutated only via the three public mutators above (immutable at runtime
    ///     except through designated entry points).
    ///   - VoidGameEvent channel (_onStreakChanged) is optional; null-safe.
    ///   - LoadSnapshot and Reset do NOT fire events (bootstrapper-safe).
    ///
    /// ── Scene / SO wiring ─────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ WinStreakSO.
    ///   2. Assign to MatchManager._winStreak.
    ///   3. Assign to GameBootstrapper._winStreak.
    ///   4. Optionally assign to PostMatchController._winStreak for HUD display.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/WinStreakSO",
        fileName = "WinStreakSO")]
    public sealed class WinStreakSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("VoidGameEvent raised whenever the streak changes (win or loss). " +
                 "Leave null if no system needs to react.")]
        [SerializeField] private VoidGameEvent _onStreakChanged;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int _currentStreak;
        private int _bestStreak;

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>Number of consecutive wins in the current run.  Always ≥ 0.</summary>
        public int CurrentStreak => _currentStreak;

        /// <summary>Highest streak ever reached.  Never decreases.  Always ≥ 0.</summary>
        public int BestStreak => _bestStreak;

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Call at the end of a won match.
        /// Increments <see cref="CurrentStreak"/>; updates <see cref="BestStreak"/>
        /// when the new value is a personal best; raises <c>_onStreakChanged</c>.
        /// </summary>
        public void RecordWin()
        {
            _currentStreak++;
            if (_currentStreak > _bestStreak)
                _bestStreak = _currentStreak;

            _onStreakChanged?.Raise();
        }

        /// <summary>
        /// Call at the end of a lost match.
        /// Resets <see cref="CurrentStreak"/> to 0; raises <c>_onStreakChanged</c>.
        /// <see cref="BestStreak"/> is never reduced.
        /// </summary>
        public void RecordLoss()
        {
            _currentStreak = 0;
            _onStreakChanged?.Raise();
        }

        /// <summary>
        /// Silent rehydration from a SaveData snapshot.  Does NOT fire
        /// <c>_onStreakChanged</c> — safe to call from <see cref="GameBootstrapper"/>.
        /// Negative values are clamped to 0.
        /// </summary>
        public void LoadSnapshot(int currentStreak, int bestStreak)
        {
            _currentStreak = Mathf.Max(0, currentStreak);
            _bestStreak    = Mathf.Max(0, bestStreak);
        }

        /// <summary>
        /// Silently clears both streak fields to 0.  Does NOT fire
        /// <c>_onStreakChanged</c>.  Intended for fresh-install resets.
        /// </summary>
        public void Reset()
        {
            _currentStreak = 0;
            _bestStreak    = 0;
        }
    }
}

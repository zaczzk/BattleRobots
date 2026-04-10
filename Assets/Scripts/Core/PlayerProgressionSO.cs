using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Tracks the player's accumulated XP and current level.
    ///
    /// ── XP threshold formula ──────────────────────────────────────────────────
    ///   Total XP required to reach level N:  50 × N × (N − 1)
    ///
    ///   Level │ Total XP needed │ XP gap from previous level
    ///   ──────┼─────────────────┼──────────────────────────
    ///     1   │       0         │  —
    ///     2   │     100         │  100
    ///     3   │     300         │  200
    ///     4   │     600         │  300
    ///     5   │    1000         │  400
    ///    10   │    4500         │  900
    ///
    /// ── Mutators ──────────────────────────────────────────────────────────────
    ///   • <see cref="AddXP"/>        — adds XP; levels up automatically; fires events.
    ///   • <see cref="LoadSnapshot"/> — silent rehydration from SaveData (no events).
    ///   • <see cref="Reset"/>        — silent clear to Level 1 / 0 XP (no events).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO mutated only via <see cref="AddXP"/> (immutable at runtime except
    ///     through the designated entry points).
    ///   - <see cref="LoadSnapshot"/> and <see cref="Reset"/> do NOT fire events
    ///     (bootstrapper-safe).
    ///   - At <see cref="MaxLevel"/>, <see cref="AddXP"/> is a no-op so accumulated
    ///     XP never grows unbounded.
    ///
    /// ── Scene / SO wiring ─────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ PlayerProgressionSO.
    ///   2. Assign to GameBootstrapper._playerProgression.
    ///   3. Assign to MatchManager._playerProgression.
    ///   4. Optionally assign to PlayerLevelController for HUD display.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/PlayerProgressionSO",
        fileName = "PlayerProgressionSO")]
    public sealed class PlayerProgressionSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Maximum level the player can reach. Must be ≥ 2.")]
        [SerializeField, Min(2)] private int _maxLevel = 10;

        [Tooltip("VoidGameEvent raised once each time the player levels up. " +
                 "Leave null if no system needs to react.")]
        [SerializeField] private VoidGameEvent _onLevelUp;

        [Tooltip("IntGameEvent raised when XP is gained. Payload = amount gained. " +
                 "Leave null if no system needs to react.")]
        [SerializeField] private IntGameEvent _onXPGained;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int _totalXP;
        private int _currentLevel = 1;

        // ── Public read-only API ──────────────────────────────────────────────

        /// <summary>Current level (1-based). Always in [1, <see cref="MaxLevel"/>].</summary>
        public int CurrentLevel => _currentLevel;

        /// <summary>Total cumulative XP ever earned. Clamped at max-level threshold when at cap.</summary>
        public int TotalXP => _totalXP;

        /// <summary>Maximum attainable level configured in the Inspector.</summary>
        public int MaxLevel => _maxLevel;

        /// <summary>True when the player has reached the maximum level.</summary>
        public bool IsMaxLevel => _currentLevel >= _maxLevel;

        /// <summary>
        /// XP accumulated above the floor threshold for the current level.
        /// Used to drive progress-bar fills.
        /// </summary>
        public int XpInCurrentLevel => _totalXP - TotalXPForLevel(_currentLevel);

        /// <summary>
        /// XP gap between the current level and the next.
        /// Returns 0 when the player is at <see cref="MaxLevel"/>.
        /// </summary>
        public int XpRequiredForNextLevel
        {
            get
            {
                if (IsMaxLevel) return 0;
                return TotalXPForLevel(_currentLevel + 1) - TotalXPForLevel(_currentLevel);
            }
        }

        /// <summary>
        /// Progress fraction [0, 1] towards the next level.
        /// Returns 1.0 when at <see cref="MaxLevel"/>.
        /// </summary>
        public float XpProgressFraction
        {
            get
            {
                if (IsMaxLevel) return 1f;
                int required = XpRequiredForNextLevel;
                return required > 0 ? (float)XpInCurrentLevel / required : 1f;
            }
        }

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Adds XP and levels up as many times as the new total warrants.
        /// <para>
        /// • Zero or negative amounts are silently ignored.
        /// • No-op when already at <see cref="MaxLevel"/>.
        /// • Fires <c>_onXPGained</c> with the amount added.
        /// • Fires <c>_onLevelUp</c> once per level gained.
        /// </para>
        /// </summary>
        /// <param name="amount">Positive XP to award.</param>
        public void AddXP(int amount)
        {
            if (amount <= 0 || IsMaxLevel) return;

            _totalXP += amount;
            _onXPGained?.Raise(amount);

            // Process level-ups — a single AddXP call can bridge multiple levels.
            while (_currentLevel < _maxLevel &&
                   _totalXP >= TotalXPForLevel(_currentLevel + 1))
            {
                _currentLevel++;
                _onLevelUp?.Raise();
            }

            // Cap stored XP at the max-level threshold to prevent unbounded growth.
            if (_currentLevel >= _maxLevel)
                _totalXP = TotalXPForLevel(_maxLevel);
        }

        /// <summary>
        /// Silently rehydrates state from a SaveData snapshot.
        /// Does NOT fire any events — safe to call from <see cref="GameBootstrapper"/>.
        /// <para>
        /// • <paramref name="totalXP"/> is clamped to ≥ 0.
        /// • <paramref name="level"/> is clamped to [1, <see cref="MaxLevel"/>].
        ///   A value of 0 (old save default) is treated as level 1.
        /// </para>
        /// </summary>
        public void LoadSnapshot(int totalXP, int level)
        {
            _totalXP      = Mathf.Max(0, totalXP);
            _currentLevel = Mathf.Clamp(level < 1 ? 1 : level, 1, _maxLevel);
        }

        /// <summary>
        /// Silently resets to Level 1 with 0 XP.
        /// Does NOT fire any events.  Intended for fresh-install resets.
        /// </summary>
        public void Reset()
        {
            _totalXP      = 0;
            _currentLevel = 1;
        }

        // ── Static helpers ────────────────────────────────────────────────────

        /// <summary>
        /// Total cumulative XP required to reach <paramref name="level"/>.
        /// Formula: <c>50 × level × (level − 1)</c>.
        /// Returns 0 for level ≤ 1.
        /// </summary>
        /// <param name="level">Target level (1-based).</param>
        public static int TotalXPForLevel(int level)
        {
            if (level <= 1) return 0;
            return 50 * level * (level - 1);
        }
    }
}

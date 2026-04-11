using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Configures time-based warning thresholds that fire SO event channels as the
    /// match timer counts down. Each threshold fires at most once per match.
    ///
    /// ── Design ────────────────────────────────────────────────────────────────────
    ///   Each <see cref="TimerThreshold"/> stores a <c>SecondsRemaining</c> value and
    ///   an optional <see cref="VoidGameEvent"/> channel to raise when the in-match
    ///   timer first crosses that threshold from above.
    ///   Once a threshold fires it is recorded in a runtime <c>HashSet</c> and will
    ///   NOT fire again until <see cref="Reset"/> is called (at match start).
    ///
    /// ── Architecture ──────────────────────────────────────────────────────────────
    ///   BattleRobots.Core namespace.  No Physics or UI references.
    ///   Immutable config at runtime; <see cref="CheckAndFire"/> mutates only the
    ///   private <c>_firedIndices</c> set (not serialized — resets between play sessions).
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────────
    ///   1. Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ MatchTimerWarning.
    ///   2. Add <see cref="TimerThreshold"/> entries (SecondsRemaining + VoidGameEvent).
    ///   3. Assign the SO to <c>MatchManager._timerWarning</c> — Reset() is called at
    ///      match start automatically.
    ///   4. Wire the <see cref="VoidGameEvent"/> channels to AudioManager, VFX, or
    ///      <see cref="BattleRobots.UI.MatchTimerWarningController"/> for panel overlays.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/MatchTimerWarning", order = 31)]
    public sealed class MatchTimerWarningSO : ScriptableObject
    {
        // ── Nested type ───────────────────────────────────────────────────────────

        /// <summary>
        /// A single time-warning entry: the threshold in seconds remaining and the
        /// optional event channel to raise when the threshold is first crossed.
        /// </summary>
        [Serializable]
        public struct TimerThreshold
        {
            [Tooltip("Fire the event when seconds remaining falls to or below this value. " +
                     "Common values: 60 (one minute), 30 (half minute), 10 (final ten).")]
            public float SecondsRemaining;

            [Tooltip("Optional SO event channel raised when this threshold is crossed. " +
                     "Wire to AudioManager, VFX handlers, or a MatchTimerWarningController. " +
                     "Leave null to fire silently (threshold is still tracked as fired).")]
            public VoidGameEvent WarningEvent;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Warning Thresholds")]
        [Tooltip("Ordered list of time thresholds. Each entry fires once per match when the " +
                 "timer reaches or drops below SecondsRemaining. Call Reset() at match start " +
                 "to allow every threshold to fire again in the next match.")]
        [SerializeField] private List<TimerThreshold> _thresholds = new List<TimerThreshold>();

        // ── Runtime state (non-serialized) ────────────────────────────────────────

        // Tracks which threshold indices have already fired in the current match.
        // HashSet: O(1) Contains + Add; not serialized so it clears between play sessions.
        private readonly HashSet<int> _firedIndices = new HashSet<int>();

        // ── Properties ────────────────────────────────────────────────────────────

        /// <summary>
        /// Ordered list of configured thresholds (read-only reference at runtime).
        /// Entries are value types; no heap allocation to read.
        /// </summary>
        public IReadOnlyList<TimerThreshold> Thresholds => _thresholds;

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates every threshold against <paramref name="secondsRemaining"/> and fires
        /// any that have not yet fired this match and whose <c>SecondsRemaining</c> is ≥ the
        /// current timer value.
        ///
        /// Zero-allocation hot path: index loop over <see cref="List{T}"/>, no LINQ.
        /// Null-safe: no-op when <c>_thresholds</c> is null or empty.
        /// A threshold whose <c>WarningEvent</c> is null fires silently (index is still tracked).
        /// </summary>
        /// <param name="secondsRemaining">Current match timer value in seconds.</param>
        public void CheckAndFire(float secondsRemaining)
        {
            if (_thresholds == null) return;

            for (int i = 0; i < _thresholds.Count; i++)
            {
                if (_firedIndices.Contains(i)) continue;

                if (secondsRemaining <= _thresholds[i].SecondsRemaining)
                {
                    _firedIndices.Add(i);
                    _thresholds[i].WarningEvent?.Raise();
                }
            }
        }

        /// <summary>
        /// Clears the fired-threshold tracker so every threshold can fire once more.
        /// Call at the start of each match (e.g. from <c>MatchManager.HandleMatchStarted</c>).
        /// Safe to call on a fresh instance with an empty threshold list.
        /// </summary>
        public void Reset() => _firedIndices.Clear();

        // ── Validation ────────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_thresholds == null) return;

            for (int i = 0; i < _thresholds.Count; i++)
            {
                if (_thresholds[i].WarningEvent == null)
                {
                    Debug.LogWarning(
                        $"[MatchTimerWarningSO] Threshold[{i}] " +
                        $"(SecondsRemaining={_thresholds[i].SecondsRemaining:F1}s) " +
                        $"has no WarningEvent assigned — it will fire silently.", this);
                }
            }
        }
#endif
    }
}

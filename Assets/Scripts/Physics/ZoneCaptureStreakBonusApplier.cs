using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that applies a score multiplier bonus when the player's zone-capture
    /// streak reaches the bonus threshold, and resets the multiplier when the streak drops
    /// or when the match ends.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   _onStreakChanged fires → ApplyBonus():
    ///     • Null-guard all data refs.
    ///     • If ZoneCaptureStreakSO.HasBonus → ScoreMultiplierSO.SetMultiplier(BonusMultiplier).
    ///     • Else                            → ScoreMultiplierSO.ResetToDefault().
    ///   _onMatchEnded fires → ResetBonus():
    ///     • ScoreMultiplierSO?.ResetToDefault().
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Physics namespace.
    ///   - DisallowMultipleComponent — one applier per robot.
    ///   - All refs optional; null refs produce a silent no-op.
    ///   - Delegates cached in Awake; zero heap allocations after initialisation.
    ///   - Must NOT reference BattleRobots.UI.
    ///
    /// Scene wiring:
    ///   _streakSO       → ZoneCaptureStreakSO (live streak state source).
    ///   _scoreMultiplier → ScoreMultiplierSO (runtime multiplier target).
    ///   _onStreakChanged → VoidGameEvent raised by ZoneCaptureStreakSO on increment/reset.
    ///   _onMatchEnded   → VoidGameEvent raised by MatchManager at match end.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneCaptureStreakBonusApplier : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Streak SO used to read HasBonus and BonusMultiplier. Leave null to disable.")]
        [SerializeField] private ZoneCaptureStreakSO _streakSO;

        [Tooltip("Runtime score multiplier SO written when the player has a streak bonus. " +
                 "Leave null to skip writing the multiplier.")]
        [SerializeField] private ScoreMultiplierSO _scoreMultiplier;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised by ZoneCaptureStreakSO on IncrementStreak and ResetStreak. " +
                 "Triggers ApplyBonus() to re-evaluate the current bonus state.")]
        [SerializeField] private VoidGameEvent _onStreakChanged;

        [Tooltip("Raised by MatchManager when the match ends. Triggers ResetBonus().")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _applyDelegate;
        private Action _resetDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _applyDelegate = ApplyBonus;
            _resetDelegate = ResetBonus;
        }

        private void OnEnable()
        {
            _onStreakChanged?.RegisterCallback(_applyDelegate);
            _onMatchEnded?.RegisterCallback(_resetDelegate);
        }

        private void OnDisable()
        {
            _onStreakChanged?.UnregisterCallback(_applyDelegate);
            _onMatchEnded?.UnregisterCallback(_resetDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Evaluates the current streak bonus state and writes to
        /// <see cref="_scoreMultiplier"/> accordingly.
        /// When <see cref="ZoneCaptureStreakSO.HasBonus"/> is true the streak's
        /// <see cref="ZoneCaptureStreakSO.BonusMultiplier"/> is applied; otherwise
        /// the multiplier is reset to its default.
        /// Null-safe on all data refs.
        /// </summary>
        public void ApplyBonus()
        {
            if (_streakSO == null || _scoreMultiplier == null) return;

            if (_streakSO.HasBonus)
                _scoreMultiplier.SetMultiplier(_streakSO.BonusMultiplier);
            else
                _scoreMultiplier.ResetToDefault();
        }

        /// <summary>
        /// Resets <see cref="_scoreMultiplier"/> to its default value.
        /// Called at match end. Null-safe.
        /// </summary>
        public void ResetBonus()
        {
            _scoreMultiplier?.ResetToDefault();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneCaptureStreakSO"/>. May be null.</summary>
        public ZoneCaptureStreakSO StreakSO => _streakSO;

        /// <summary>The assigned <see cref="ScoreMultiplierSO"/>. May be null.</summary>
        public ScoreMultiplierSO ScoreMultiplier => _scoreMultiplier;
    }
}

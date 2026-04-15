using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that applies a score multiplier bonus when the player holds
    /// zone dominance at match start, and resets the multiplier at match end.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   _onMatchStarted fires → Apply():
    ///     • Null-guard all data refs.
    ///     • Check ZoneDominanceSO.HasDominance.
    ///     • If true → ScoreMultiplierSO.SetMultiplier(_bonusMultiplier).
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
    ///   _dominanceSO     → ZoneDominanceSO (zone dominance state source).
    ///   _scoreMultiplier → ScoreMultiplierSO (runtime multiplier target).
    ///   _onMatchStarted  → VoidGameEvent raised by MatchManager at match start.
    ///   _onMatchEnded    → VoidGameEvent raised by MatchManager at match end.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneDominanceBonusApplier : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Zone dominance SO used to check HasDominance at match start. Leave null to disable.")]
        [SerializeField] private ZoneDominanceSO _dominanceSO;

        [Tooltip("Runtime score multiplier SO written when player has dominance. Leave null to skip.")]
        [SerializeField] private ScoreMultiplierSO _scoreMultiplier;

        [Header("Bonus Settings")]
        [Tooltip("Score multiplier applied when the player holds zone dominance. " +
                 "Must be ≥ 1 — set to 1 for no bonus effect.")]
        [SerializeField, Min(1f)] private float _bonusMultiplier = 1.5f;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised by MatchManager when the match begins. Triggers Apply().")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("Raised by MatchManager when the match ends. Triggers ResetBonus().")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _applyDelegate;
        private Action _resetDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _applyDelegate = Apply;
            _resetDelegate = ResetBonus;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_applyDelegate);
            _onMatchEnded?.RegisterCallback(_resetDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_applyDelegate);
            _onMatchEnded?.UnregisterCallback(_resetDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Checks whether the player has zone dominance and, if so, writes
        /// <see cref="_bonusMultiplier"/> to <see cref="_scoreMultiplier"/>.
        /// Null-safe on all data refs.
        /// </summary>
        public void Apply()
        {
            if (_dominanceSO == null || _scoreMultiplier == null) return;

            if (_dominanceSO.HasDominance)
                _scoreMultiplier.SetMultiplier(_bonusMultiplier);
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

        /// <summary>The assigned <see cref="ZoneDominanceSO"/>. May be null.</summary>
        public ZoneDominanceSO DominanceSO => _dominanceSO;

        /// <summary>The assigned <see cref="ScoreMultiplierSO"/>. May be null.</summary>
        public ScoreMultiplierSO ScoreMultiplier => _scoreMultiplier;

        /// <summary>The configured dominance bonus multiplier (≥ 1).</summary>
        public float BonusMultiplier => _bonusMultiplier;
    }
}

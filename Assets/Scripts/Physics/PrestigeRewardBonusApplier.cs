using System;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Applies the prestige reward score multiplier at match start by looking up the
    /// player's current prestige rank in <see cref="PrestigeRewardCatalogSO"/> and
    /// writing the <see cref="PrestigeRewardEntry.bonusMultiplier"/> to a shared
    /// <see cref="ScoreMultiplierSO"/>. Resets the multiplier at match end.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   _onMatchStarted fires → Apply():
    ///     • Null-guard all three data refs.
    ///     • TryGetRewardForRank(prestigeCount) → false → no-op.
    ///     • true → ScoreMultiplierSO.SetMultiplier(entry.bonusMultiplier).
    ///   _onMatchEnded fires → ResetMultiplier():
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
    ///   _catalog         → PrestigeRewardCatalogSO (per-rank rewards).
    ///   _prestigeSystem  → PrestigeSystemSO (current prestige count).
    ///   _scoreMultiplier → ScoreMultiplierSO (runtime multiplier target).
    ///   _onMatchStarted  → VoidGameEvent raised by MatchManager at match start.
    ///   _onMatchEnded    → VoidGameEvent raised by MatchManager at match end.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrestigeRewardBonusApplier : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Catalog of per-prestige-rank rewards. Leave null to disable bonus.")]
        [SerializeField] private PrestigeRewardCatalogSO _catalog;

        [Tooltip("Runtime prestige SO. Provides the current prestige count.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        [Tooltip("Runtime score multiplier SO written at match start. Leave null to skip.")]
        [SerializeField] private ScoreMultiplierSO _scoreMultiplier;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("Raised by MatchManager when the match begins. Triggers Apply().")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Tooltip("Raised by MatchManager when the match ends. Triggers ResetMultiplier().")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _applyDelegate;
        private Action _resetDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _applyDelegate = Apply;
            _resetDelegate = ResetMultiplier;
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
        /// Looks up the reward for the player's current prestige rank. If found,
        /// writes its <see cref="PrestigeRewardEntry.bonusMultiplier"/> to
        /// <see cref="_scoreMultiplier"/>. Null-safe on all three data refs.
        /// </summary>
        public void Apply()
        {
            if (_catalog == null || _prestigeSystem == null || _scoreMultiplier == null)
                return;

            int prestigeCount = _prestigeSystem.PrestigeCount;
            if (!_catalog.TryGetRewardForRank(prestigeCount, out PrestigeRewardEntry entry))
                return;

            _scoreMultiplier.SetMultiplier(entry.bonusMultiplier);
        }

        /// <summary>
        /// Resets <see cref="_scoreMultiplier"/> to its default value.
        /// Called at match end. Null-safe.
        /// </summary>
        public void ResetMultiplier()
        {
            _scoreMultiplier?.ResetToDefault();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="PrestigeRewardCatalogSO"/>. May be null.</summary>
        public PrestigeRewardCatalogSO Catalog => _catalog;

        /// <summary>The assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;

        /// <summary>The assigned <see cref="ScoreMultiplierSO"/>. May be null.</summary>
        public ScoreMultiplierSO ScoreMultiplier => _scoreMultiplier;
    }
}

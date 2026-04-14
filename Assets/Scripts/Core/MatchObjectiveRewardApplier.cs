using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Grants the <see cref="MatchBonusObjectiveSO.BonusReward"/> to the
    /// <see cref="PlayerWallet"/> each time an objective is completed, and records the
    /// outcome in <see cref="MatchObjectivePersistenceSO"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   1. <see cref="_onObjectiveCompleted"/> fires (raised by MatchObjectiveTrackerSO
    ///      once per successful objective).
    ///   2. This MB calls <see cref="ApplyReward"/>:
    ///      a. Reads <c>BonusReward</c> / <c>BonusTitle</c> from <see cref="_bonusObjective"/>.
    ///      b. Calls <see cref="PlayerWallet.AddFunds"/> when reward &gt; 0.
    ///      c. Calls <see cref="MatchObjectivePersistenceSO.Record"/> to persist the entry.
    ///      d. Raises <c>_onRewardApplied</c>.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace — no Physics / UI references.
    ///   - DisallowMultipleComponent — one applier per match context.
    ///   - Delegate cached in Awake; zero heap allocation after initialisation.
    ///   - All inspector fields optional — safe with any subset assigned.
    ///
    /// Scene wiring:
    ///   _bonusObjective       → MatchBonusObjectiveSO (title + reward amount).
    ///   _persistence          → MatchObjectivePersistenceSO ring-buffer.
    ///   _wallet               → PlayerWallet runtime SO.
    ///   _onObjectiveCompleted → same VoidGameEvent as MatchObjectiveTrackerSO._onObjectiveCompleted.
    ///   _onRewardApplied      → optional out channel for VFX / audio on reward grant.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchObjectiveRewardApplier : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Provides BonusTitle and BonusReward used when crediting and recording.")]
        [SerializeField] private MatchBonusObjectiveSO _bonusObjective;

        [Tooltip("Ring-buffer SO that persists each objective outcome for post-match review.")]
        [SerializeField] private MatchObjectivePersistenceSO _persistence;

        [Tooltip("Player's runtime wallet. AddFunds is called once per completion when reward > 0.")]
        [SerializeField] private PlayerWallet _wallet;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent raised by MatchObjectiveTrackerSO once per objective completion. " +
                 "Triggers ApplyReward.")]
        [SerializeField] private VoidGameEvent _onObjectiveCompleted;

        [Header("Event Channel — Out (optional)")]
        [Tooltip("Raised after the reward is applied and the outcome is persisted.")]
        [SerializeField] private VoidGameEvent _onRewardApplied;

        // ── Private state ─────────────────────────────────────────────────────

        private Action _applyDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _applyDelegate = ApplyReward;
        }

        private void OnEnable()
        {
            _onObjectiveCompleted?.RegisterCallback(_applyDelegate);
        }

        private void OnDisable()
        {
            _onObjectiveCompleted?.UnregisterCallback(_applyDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Credits <see cref="PlayerWallet"/> with <see cref="MatchBonusObjectiveSO.BonusReward"/>
        /// (when reward &gt; 0) and records the completed outcome in
        /// <see cref="MatchObjectivePersistenceSO"/>.
        /// No-op when <see cref="_bonusObjective"/> is null.
        /// Null-safe on all other fields. Zero allocation.
        /// </summary>
        public void ApplyReward()
        {
            if (_bonusObjective == null) return;

            int    reward = _bonusObjective.BonusReward;
            string title  = _bonusObjective.BonusTitle;

            if (reward > 0)
                _wallet?.AddFunds(reward);

            _persistence?.Record(title, completed: true, reward: reward);
            _onRewardApplied?.Raise();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MatchBonusObjectiveSO"/>. May be null.</summary>
        public MatchBonusObjectiveSO BonusObjective => _bonusObjective;

        /// <summary>The assigned <see cref="MatchObjectivePersistenceSO"/>. May be null.</summary>
        public MatchObjectivePersistenceSO Persistence => _persistence;

        /// <summary>The assigned <see cref="PlayerWallet"/>. May be null.</summary>
        public PlayerWallet Wallet => _wallet;
    }
}

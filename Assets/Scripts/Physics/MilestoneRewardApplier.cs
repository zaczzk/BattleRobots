using System;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Physics MonoBehaviour that grants a flat currency reward from
    /// <see cref="MilestoneRewardCatalogSO"/> each time the player clears a new
    /// <see cref="MasteryProgressMilestoneSO"/> milestone threshold.
    ///
    /// ── Mechanic ──────────────────────────────────────────────────────────────────
    ///   • Subscribes to <c>_onMatchEnded</c> — damage accumulations are updated by
    ///     <see cref="DamageTypeMasterySO.AddDealtFromStats"/> inside MatchManager at
    ///     match end, so this is the earliest safe moment to check for newly-cleared
    ///     milestones.
    ///   • On each check, per-type cleared counts are compared against a snapshot taken
    ///     at the previous check.  Newly cleared milestones (prevCleared..currentCleared-1)
    ///     trigger a <see cref="PlayerWallet.AddFunds"/> call and raise _onRewardGranted.
    ///   • <c>_previousClearedCounts</c> is pre-allocated in Awake — zero GC after init.
    ///   • All component SO references are optional; null refs are silently skipped.
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────────
    ///   • BattleRobots.Physics namespace.
    ///   • DisallowMultipleComponent — one applier per robot root.
    ///   • No Update / FixedUpdate loop.
    ///   • Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _catalog         → MilestoneRewardCatalogSO with per-milestone currency rewards.
    ///   _milestoneSO     → MasteryProgressMilestoneSO defining the threshold array.
    ///   _mastery         → shared DamageTypeMasterySO tracking live accumulations.
    ///   _wallet          → shared PlayerWallet to credit the reward.
    ///   _onMatchEnded    → VoidGameEvent raised at match end by MatchManager.
    ///   _onRewardGranted → VoidGameEvent raised once per milestone reward granted.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MilestoneRewardApplier : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Catalog listing the flat currency reward per milestone index. " +
                 "Leave null to disable all rewards.")]
        [SerializeField] private MilestoneRewardCatalogSO _catalog;

        [Tooltip("Milestone threshold SO whose GetClearedCount drives the reward check. " +
                 "Leave null to skip checks.")]
        [SerializeField] private MasteryProgressMilestoneSO _milestoneSO;

        [Tooltip("Live mastery SO providing per-type damage accumulations. " +
                 "Leave null to skip checks.")]
        [SerializeField] private DamageTypeMasterySO _mastery;

        [Tooltip("Player wallet to credit rewards to. " +
                 "Leave null to suppress currency grants while still raising _onRewardGranted.")]
        [SerializeField] private PlayerWallet _wallet;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channel — In")]
        [Tooltip("Raised at match end.  Triggers the milestone check. " +
                 "Leave null to disable automatic checks.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("Event Channel — Out")]
        [Tooltip("Raised once per milestone reward granted. " +
                 "Leave null if no system needs to react.")]
        [SerializeField] private VoidGameEvent _onRewardGranted;

        // ── Internal state ────────────────────────────────────────────────────

        private static readonly DamageType[] s_types =
        {
            DamageType.Physical,
            DamageType.Energy,
            DamageType.Thermal,
            DamageType.Shock
        };

        private int[]  _previousClearedCounts;
        private Action _checkDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _checkDelegate         = CheckMilestones;
            _previousClearedCounts = new int[s_types.Length];
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_checkDelegate);
            SnapshotClearedCounts();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_checkDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Snapshots the current cleared-milestone counts per damage type so that the
        /// next <see cref="CheckMilestones"/> call can detect newly cleared milestones.
        /// Silent no-op when <see cref="_mastery"/> or <see cref="_milestoneSO"/> is null.
        /// </summary>
        private void SnapshotClearedCounts()
        {
            if (_mastery == null || _milestoneSO == null) return;

            for (int i = 0; i < s_types.Length; i++)
            {
                float accum = _mastery.GetAccumulation(s_types[i]);
                _previousClearedCounts[i] = _milestoneSO.GetClearedCount(s_types[i], accum);
            }
        }

        /// <summary>
        /// Checks all four damage types for newly-cleared milestones, grants the
        /// configured currency reward for each, and updates the snapshot.
        /// Silent no-op when <see cref="_catalog"/>, <see cref="_mastery"/>, or
        /// <see cref="_milestoneSO"/> is null.
        /// </summary>
        private void CheckMilestones()
        {
            if (_catalog == null || _mastery == null || _milestoneSO == null) return;

            for (int i = 0; i < s_types.Length; i++)
            {
                float accum          = _mastery.GetAccumulation(s_types[i]);
                int   currentCleared = _milestoneSO.GetClearedCount(s_types[i], accum);
                int   prevCleared    = _previousClearedCounts[i];

                for (int j = prevCleared; j < currentCleared; j++)
                {
                    float reward = _catalog.GetReward(j);
                    if (reward > 0f && _wallet != null)
                    {
                        _wallet.AddFunds(Mathf.RoundToInt(reward));
                        _onRewardGranted?.Raise();
                    }
                }

                _previousClearedCounts[i] = currentCleared;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MilestoneRewardCatalogSO"/>. May be null.</summary>
        public MilestoneRewardCatalogSO Catalog => _catalog;

        /// <summary>The assigned <see cref="PlayerWallet"/>. May be null.</summary>
        public PlayerWallet Wallet => _wallet;
    }
}

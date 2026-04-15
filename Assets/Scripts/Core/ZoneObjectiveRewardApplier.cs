using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour that listens for zone-objective completion and credits the
    /// player wallet using a <see cref="ZoneObjectiveRewardConfig"/>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   _onObjectiveComplete fires → ApplyReward():
    ///     1. Null-guards _rewardConfig + _wallet. No-op when either is missing.
    ///     2. If _objectiveSO is assigned and NOT complete → no-op (guards against
    ///        spurious channel raises before objective evaluation finishes).
    ///     3. Reads playerZoneCount from _dominanceSO?.PlayerZoneCount (0 if null).
    ///     4. Computes reward = _rewardConfig.GetReward(zonesHeld).
    ///     5. Credits _wallet.AddFunds(Mathf.RoundToInt(reward)) when reward > 0.
    ///     6. Raises _onRewardApplied (optional out-channel).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All inspector refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///   - DisallowMultipleComponent — one applier per game-object.
    ///
    /// Scene wiring:
    ///   _rewardConfig        → ZoneObjectiveRewardConfig asset.
    ///   _dominanceSO         → ZoneDominanceSO (for per-zone bonus; optional).
    ///   _wallet              → PlayerWallet SO (currency target).
    ///   _objectiveSO         → ZoneObjectiveSO (guard check; optional).
    ///   _onObjectiveComplete → VoidGameEvent raised when the zone objective completes
    ///                          (wired to ZoneObjectiveSO._onObjectiveComplete).
    ///   _onRewardApplied     → Optional out-channel for UI notification controllers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneObjectiveRewardApplier : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Reward configuration SO defining base reward and per-zone bonus.")]
        [SerializeField] private ZoneObjectiveRewardConfig _rewardConfig;

        [Tooltip("Zone dominance SO providing the player's current zone count. " +
                 "Optional — pass 0 zones to GetReward when null.")]
        [SerializeField] private ZoneDominanceSO _dominanceSO;

        [Tooltip("Player wallet SO to credit when the reward is applied.")]
        [SerializeField] private PlayerWallet _wallet;

        [Tooltip("Zone objective SO used as a completion guard. " +
                 "When assigned, ApplyReward no-ops if IsComplete is false.")]
        [SerializeField] private ZoneObjectiveSO _objectiveSO;

        // ── Inspector — Event Channels ────────────────────────────────────────

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised by ZoneObjectiveSO when the zone objective is complete. " +
                 "Wire to ZoneObjectiveSO._onObjectiveComplete.")]
        [SerializeField] private VoidGameEvent _onObjectiveComplete;

        [Header("Event Channels — Out (optional)")]
        [Tooltip("Raised after the reward is successfully applied to the wallet.")]
        [SerializeField] private VoidGameEvent _onRewardApplied;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _applyRewardDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _applyRewardDelegate = ApplyReward;
        }

        private void OnEnable()
        {
            _onObjectiveComplete?.RegisterCallback(_applyRewardDelegate);
        }

        private void OnDisable()
        {
            _onObjectiveComplete?.UnregisterCallback(_applyRewardDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Credits the player wallet with the configured reward scaled by the
        /// number of zones held at the moment of calling.
        /// Guards against null config/wallet and optional objective IsComplete check.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void ApplyReward()
        {
            if (_rewardConfig == null || _wallet == null) return;

            // Optional guard: if an objective SO is assigned, only pay out when complete.
            if (_objectiveSO != null && !_objectiveSO.IsComplete) return;

            int   zonesHeld = _dominanceSO != null ? _dominanceSO.PlayerZoneCount : 0;
            float reward    = _rewardConfig.GetReward(zonesHeld);
            int   amount    = Mathf.RoundToInt(reward);

            if (amount > 0)
                _wallet.AddFunds(amount);

            _onRewardApplied?.Raise();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneObjectiveRewardConfig"/>. May be null.</summary>
        public ZoneObjectiveRewardConfig RewardConfig => _rewardConfig;

        /// <summary>The assigned <see cref="ZoneObjectiveSO"/> guard. May be null.</summary>
        public ZoneObjectiveSO ObjectiveSO => _objectiveSO;
    }
}

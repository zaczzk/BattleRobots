using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks cumulative zone-capture progression
    /// against a set of configurable gate thresholds.
    ///
    /// ── Gate logic ─────────────────────────────────────────────────────────────
    ///   Each entry in <see cref="_gateThresholds"/> is a minimum total zone count
    ///   required to unlock the corresponding tier.  Thresholds should be supplied
    ///   in ascending order.  <see cref="EvaluateGates"/> is idempotent — calling
    ///   it multiple times with the same value never fires the event twice for an
    ///   already-unlocked tier.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — relies on LoadSnapshot for persistence.
    ///   - Zero heap allocation on <see cref="EvaluateGates"/> hot path.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlProgressionGate.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlProgressionGate", order = 25)]
    public sealed class ZoneControlProgressionGateSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Gate Thresholds (ascending zone counts)")]
        [Tooltip("Minimum cumulative zones captured to unlock each tier. " +
                 "Supply in ascending order.")]
        [SerializeField] private int[] _gateThresholds = { 5, 15, 30, 50, 75 };

        [Header("Event Channels (optional)")]
        [Tooltip("Raised each time a new tier is unlocked via EvaluateGates.")]
        [SerializeField] private VoidGameEvent _onGateUnlocked;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int _unlockedTiers;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Number of tiers currently unlocked.</summary>
        public int UnlockedTiers => _unlockedTiers;

        /// <summary>Total number of gate tiers defined in the inspector.</summary>
        public int GateCount => _gateThresholds != null ? _gateThresholds.Length : 0;

        /// <summary>
        /// The zone-capture count required to unlock the next tier.
        /// Returns <c>-1</c> when all tiers are already unlocked or none are defined.
        /// </summary>
        public int NextThreshold
        {
            get
            {
                if (_gateThresholds == null || _unlockedTiers >= _gateThresholds.Length)
                    return -1;
                return _gateThresholds[_unlockedTiers];
            }
        }

        /// <summary>True when every defined gate tier has been unlocked.</summary>
        public bool AllUnlocked =>
            _gateThresholds == null || _gateThresholds.Length == 0 ||
            _unlockedTiers >= _gateThresholds.Length;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Checks <paramref name="totalZonesCaptured"/> against all defined thresholds
        /// and fires <c>_onGateUnlocked</c> for each tier newly crossed.
        /// Already-unlocked tiers are skipped (idempotent).
        /// Zero heap allocation.
        /// </summary>
        /// <param name="totalZonesCaptured">
        /// Cumulative zones captured (sourced from
        /// <see cref="ZoneControlSessionSummarySO.TotalZonesCaptured"/>).
        /// </param>
        public void EvaluateGates(int totalZonesCaptured)
        {
            if (_gateThresholds == null) return;

            for (int i = _unlockedTiers; i < _gateThresholds.Length; i++)
            {
                if (totalZonesCaptured >= _gateThresholds[i])
                {
                    _unlockedTiers = i + 1;
                    _onGateUnlocked?.Raise();
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Restores the previously persisted unlocked tier count.
        /// Bootstrapper-safe; clamped to [0, GateCount].
        /// Does not fire any events.
        /// </summary>
        public void LoadSnapshot(int unlockedTiers)
        {
            _unlockedTiers = Mathf.Clamp(unlockedTiers, 0, GateCount);
        }

        /// <summary>Returns the current unlocked tier count for persistence.</summary>
        public int TakeSnapshot() => _unlockedTiers;

        /// <summary>
        /// Resets the unlocked tier counter to zero.
        /// Does not fire any events.
        /// </summary>
        public void Reset()
        {
            _unlockedTiers = 0;
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (_gateThresholds == null) return;
            for (int i = 0; i < _gateThresholds.Length; i++)
                _gateThresholds[i] = Mathf.Max(0, _gateThresholds[i]);
        }
    }
}

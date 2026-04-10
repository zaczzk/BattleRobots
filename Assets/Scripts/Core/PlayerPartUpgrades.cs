using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks the upgrade tier for each part the player has upgraded.
    ///
    /// ── Lifecycle ────────────────────────────────────────────────────────────
    ///   • <see cref="GameBootstrapper"/> calls <see cref="LoadSnapshot"/> on startup
    ///     to restore persisted tier data from <see cref="SaveData.upgradePartIds"/> /
    ///     <see cref="SaveData.upgradePartTierValues"/>.
    ///   • <see cref="BattleRobots.UI.UpgradeManager"/> calls <see cref="SetTier"/> after
    ///     a successful upgrade and persists the snapshot to disk.
    ///   • <see cref="RobotStatsAggregator.Compute(RobotDefinition,
    ///     IEnumerable{PartDefinition}, PlayerPartUpgrades, PartUpgradeConfig)"/>
    ///     reads <see cref="GetTier"/> for each equipped part to scale its stats.
    ///
    /// ── Data model ───────────────────────────────────────────────────────────
    ///   JsonUtility cannot serialise <c>Dictionary&lt;string,int&gt;</c>, so the data
    ///   is stored in parallel <c>_partIds</c> / <c>_tierValues</c> lists that are kept
    ///   in sync with a runtime <c>Dictionary</c> mirror for O(1) lookups.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Economy ▶ PlayerPartUpgrades.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Economy/PlayerPartUpgrades", order = 2)]
    public sealed class PlayerPartUpgrades : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels — Out")]
        [Tooltip("Raised after any tier change. Wire to upgrade UI for automatic refresh.")]
        [SerializeField] private VoidGameEvent _onUpgradesChanged;

        // ── Serialised state (parallel lists — JsonUtility-friendly) ──────────
        [SerializeField] private List<string> _partIds    = new List<string>();
        [SerializeField] private List<int>    _tierValues = new List<int>();

        // ── Runtime mirror ────────────────────────────────────────────────────
        private readonly Dictionary<string, int> _lookup = new Dictionary<string, int>();
        private bool _initialized;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Number of parts that have at least one upgrade tier applied.</summary>
        public int Count
        {
            get
            {
                if (!_initialized) BuildLookup();
                return _lookup.Count;
            }
        }

        /// <summary>
        /// Returns the upgrade tier for <paramref name="partId"/>.
        /// Returns 0 for unknown or null/empty IDs.
        /// </summary>
        public int GetTier(string partId)
        {
            if (!_initialized) BuildLookup();
            if (string.IsNullOrEmpty(partId)) return 0;
            return _lookup.TryGetValue(partId, out int tier) ? tier : 0;
        }

        /// <summary>
        /// Stores <paramref name="tier"/> for <paramref name="partId"/>.
        /// Negative values are clamped to 0.
        /// Fires <c>_onUpgradesChanged</c>.
        /// Silently ignores null/empty IDs.
        /// </summary>
        public void SetTier(string partId, int tier)
        {
            if (string.IsNullOrEmpty(partId)) return;
            if (!_initialized) BuildLookup();
            _lookup[partId] = Mathf.Max(0, tier);
            SyncToLists();
            _onUpgradesChanged?.Raise();
        }

        /// <summary>
        /// Rehydrates from parallel persisted lists loaded from
        /// <see cref="SaveData.upgradePartIds"/> and <see cref="SaveData.upgradePartTierValues"/>.
        /// Safe with null or mismatched lists (excess entries are ignored).
        /// Does NOT fire the event (bootstrapper-safe).
        /// </summary>
        public void LoadSnapshot(List<string> keys, List<int> values)
        {
            _lookup.Clear();
            if (keys != null && values != null)
            {
                int len = Mathf.Min(keys.Count, values.Count);
                for (int i = 0; i < len; i++)
                {
                    if (!string.IsNullOrEmpty(keys[i]))
                        _lookup[keys[i]] = Mathf.Max(0, values[i]);
                }
            }
            SyncToLists();
            _initialized = true;
        }

        /// <summary>
        /// Copies current tier data into new lists suitable for writing directly to
        /// <see cref="SaveData.upgradePartIds"/> and <see cref="SaveData.upgradePartTierValues"/>.
        /// </summary>
        public void TakeSnapshot(out List<string> keys, out List<int> values)
        {
            if (!_initialized) BuildLookup();
            keys   = new List<string>(_partIds);
            values = new List<int>(_tierValues);
        }

        /// <summary>
        /// Clears all upgrade data. Does NOT fire the event (bootstrapper-safe).
        /// </summary>
        public void Reset()
        {
            _lookup.Clear();
            _partIds.Clear();
            _tierValues.Clear();
            _initialized = true;
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void BuildLookup()
        {
            _lookup.Clear();
            int len = Mathf.Min(_partIds.Count, _tierValues.Count);
            for (int i = 0; i < len; i++)
            {
                if (!string.IsNullOrEmpty(_partIds[i]))
                    _lookup[_partIds[i]] = _tierValues[i];
            }
            _initialized = true;
        }

        /// <summary>Keeps parallel lists in sync with the runtime Dictionary after any mutation.</summary>
        private void SyncToLists()
        {
            _partIds.Clear();
            _tierValues.Clear();
            foreach (var kvp in _lookup)
            {
                _partIds.Add(kvp.Key);
                _tierValues.Add(kvp.Value);
            }
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onUpgradesChanged == null)
                Debug.LogWarning(
                    "[PlayerPartUpgrades] _onUpgradesChanged not assigned — " +
                    "upgrade UI will not refresh automatically.", this);
        }
#endif
    }
}

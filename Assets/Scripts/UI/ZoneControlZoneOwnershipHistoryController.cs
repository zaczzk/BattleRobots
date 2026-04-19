using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneOwnershipHistoryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneOwnershipHistorySO    _historySO;
        [SerializeField] private ZoneControlZoneControllerCatalogSO   _catalogSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneControlChanged;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSnapshotAdded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _snapshotLabel;
        [SerializeField] private Text       _majorityLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneControlChangedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSnapshotAddedDelegate;

        private void Awake()
        {
            _handleZoneControlChangedDelegate = HandleZoneControlChanged;
            _handleMatchStartedDelegate       = HandleMatchStarted;
            _handleSnapshotAddedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onZoneControlChanged?.RegisterCallback(_handleZoneControlChangedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSnapshotAdded?.RegisterCallback(_handleSnapshotAddedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneControlChanged?.UnregisterCallback(_handleZoneControlChangedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSnapshotAdded?.UnregisterCallback(_handleSnapshotAddedDelegate);
        }

        private void HandleZoneControlChanged()
        {
            if (_historySO == null) return;
            int playerOwned = _catalogSO?.PlayerOwnedCount ?? 0;
            int totalZones  = _catalogSO?.ZoneCount        ?? 0;
            _historySO.TakeSnapshot(playerOwned, totalZones);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _historySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_historySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_snapshotLabel != null)
                _snapshotLabel.text = $"Snapshots: {_historySO.SnapshotCount}";

            if (_majorityLabel != null)
                _majorityLabel.text = $"Majority: {_historySO.GetMajorityCount()}";
        }

        public ZoneControlZoneOwnershipHistorySO HistorySO => _historySO;
    }
}

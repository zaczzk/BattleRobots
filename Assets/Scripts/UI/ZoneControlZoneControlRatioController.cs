using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneControlRatioController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneControlRatioSO      _ratioSO;
        [SerializeField] private ZoneControlZoneControllerCatalogSO _catalogSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onControlChanged;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMajorityChanged;
        [SerializeField] private VoidGameEvent _onRatioUpdated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _ratioLabel;
        [SerializeField] private Slider     _ratioBar;
        [SerializeField] private GameObject _majorityBadge;
        [SerializeField] private GameObject _panel;

        private Action _handleControlChangedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleControlChangedDelegate = HandleControlChanged;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _refreshDelegate              = Refresh;
        }

        private void OnEnable()
        {
            _onControlChanged?.RegisterCallback(_handleControlChangedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMajorityChanged?.RegisterCallback(_refreshDelegate);
            _onRatioUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onControlChanged?.UnregisterCallback(_handleControlChangedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMajorityChanged?.UnregisterCallback(_refreshDelegate);
            _onRatioUpdated?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleControlChanged()
        {
            if (_ratioSO == null) return;
            int player = _catalogSO != null ? _catalogSO.PlayerOwnedCount : 0;
            int total  = _catalogSO != null ? _catalogSO.ZoneCount        : 0;
            _ratioSO.SetZoneCounts(player, total);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _ratioSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_ratioSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_ratioLabel != null)
                _ratioLabel.text = $"Control: {Mathf.RoundToInt(_ratioSO.HoldRatio * 100)}%";

            if (_ratioBar != null)
                _ratioBar.value = _ratioSO.HoldRatio;

            if (_majorityBadge != null)
                _majorityBadge.SetActive(_ratioSO.HasMajority);
        }

        public ZoneControlZoneControlRatioSO RatioSO => _ratioSO;
    }
}

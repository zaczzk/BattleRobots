using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureHarvestController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureHarvestSO _harvestSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHarvest;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _seasonLabel;
        [SerializeField] private Text       _harvestLabel;
        [SerializeField] private Slider     _seasonBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleHarvestDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleHarvestDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHarvest?.RegisterCallback(_handleHarvestDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHarvest?.UnregisterCallback(_handleHarvestDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_harvestSO == null) return;
            int bonus = _harvestSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_harvestSO == null) return;
            _harvestSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _harvestSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_harvestSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_seasonLabel != null)
                _seasonLabel.text = $"Season: {_harvestSO.SeasonCaptures}/{_harvestSO.CapturesPerSeason}";

            if (_harvestLabel != null)
                _harvestLabel.text = $"Harvests: {_harvestSO.HarvestCount}";

            if (_seasonBar != null)
                _seasonBar.value = _harvestSO.SeasonProgress;
        }

        public ZoneControlCaptureHarvestSO HarvestSO => _harvestSO;
    }
}

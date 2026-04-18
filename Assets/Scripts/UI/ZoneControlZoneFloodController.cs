using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneFloodController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneFloodSO              _floodSO;
        [SerializeField] private ZoneControlZoneControllerCatalogSO  _catalogSO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFloodDetected;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _floodLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFloodDetectedDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleFloodDetectedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFloodDetected?.RegisterCallback(_handleFloodDetectedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFloodDetected?.UnregisterCallback(_handleFloodDetectedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_floodSO == null) return;
            int prevFloodCount = _floodSO.FloodCount;
            int ownedCount     = _catalogSO != null ? _catalogSO.PlayerOwnedCount : 0;
            _floodSO.RecordCapture(ownedCount);
            if (_floodSO.FloodCount > prevFloodCount)
                _wallet?.AddFunds(_floodSO.BonusPerFlood);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_floodSO == null) return;
            int ownedCount = _catalogSO != null ? _catalogSO.PlayerOwnedCount : 0;
            _floodSO.RecordLoss(ownedCount);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _floodSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_floodSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _floodSO.IsFlooded ? "FLOOD!" : "Standby";

            if (_floodLabel != null)
                _floodLabel.text = $"Floods: {_floodSO.FloodCount}";
        }

        public ZoneControlZoneFloodSO FloodSO => _floodSO;
    }
}

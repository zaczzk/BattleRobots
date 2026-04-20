using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRadarController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRadarSO _radarSO;
        [SerializeField] private PlayerWalletSO            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRadarPing;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _windowLabel;
        [SerializeField] private Text       _pingCountLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePingDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handlePingDelegate         = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRadarPing?.RegisterCallback(_handlePingDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRadarPing?.UnregisterCallback(_handlePingDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_radarSO == null) return;
            int prev = _radarSO.PingCount;
            _radarSO.RecordPlayerCapture();
            if (_radarSO.PingCount > prev)
                _wallet?.AddFunds(_radarSO.BonusPerPing);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_radarSO == null) return;
            _radarSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _radarSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_radarSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_windowLabel != null)
                _windowLabel.text = $"P: {_radarSO.WindowPlayerCaptures} / B: {_radarSO.WindowBotCaptures}";

            if (_pingCountLabel != null)
                _pingCountLabel.text = $"Pings: {_radarSO.PingCount}";
        }

        public ZoneControlCaptureRadarSO RadarSO => _radarSO;
    }
}

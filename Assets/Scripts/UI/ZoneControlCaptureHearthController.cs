using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureHearthController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureHearthSO _hearthSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onIgnite;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _logLabel;
        [SerializeField] private Text       _igniteLabel;
        [SerializeField] private Slider     _logBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleIgniteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleIgniteDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onIgnite?.RegisterCallback(_handleIgniteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onIgnite?.UnregisterCallback(_handleIgniteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_hearthSO == null) return;
            int bonus = _hearthSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_hearthSO == null) return;
            _hearthSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _hearthSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_hearthSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_logLabel != null)
                _logLabel.text = $"Logs: {_hearthSO.LogCount}/{_hearthSO.MaxLogs}";

            if (_igniteLabel != null)
                _igniteLabel.text = $"Ignites: {_hearthSO.IgniteCount}";

            if (_logBar != null)
                _logBar.value = _hearthSO.LogProgress;
        }

        public ZoneControlCaptureHearthSO HearthSO => _hearthSO;
    }
}

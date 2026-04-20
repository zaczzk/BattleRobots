using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureEmberController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureEmberSO _emberSO;
        [SerializeField] private PlayerWalletSO             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onIgnite;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _heatLabel;
        [SerializeField] private Text       _igniteCountLabel;
        [SerializeField] private Slider     _emberBar;
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
            if (_emberSO == null) return;
            int bonus = _emberSO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_emberSO == null) return;
            _emberSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _emberSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_emberSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_heatLabel != null)
                _heatLabel.text = $"Embers: {_emberSO.CurrentHeat:F0}/{_emberSO.IgniteThreshold:F0}";

            if (_igniteCountLabel != null)
                _igniteCountLabel.text = $"Ignites: {_emberSO.IgniteCount}";

            if (_emberBar != null)
                _emberBar.value = _emberSO.EmberProgress;
        }

        public ZoneControlCaptureEmberSO EmberSO => _emberSO;
    }
}

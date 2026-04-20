using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSpireController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSpireSO _spireSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSpireChanneled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _energyLabel;
        [SerializeField] private Text       _channelLabel;
        [SerializeField] private Slider     _energyBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSpireChanneledDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleSpireChanneledDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSpireChanneled?.RegisterCallback(_handleSpireChanneledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSpireChanneled?.UnregisterCallback(_handleSpireChanneledDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_spireSO == null) return;
            int bonus = _spireSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_spireSO == null) return;
            _spireSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _spireSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_spireSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_energyLabel != null)
                _energyLabel.text = $"Energy: {_spireSO.Energy}/{_spireSO.ChannelsNeeded}";

            if (_channelLabel != null)
                _channelLabel.text = $"Channels: {_spireSO.ChannelCount}";

            if (_energyBar != null)
                _energyBar.value = _spireSO.EnergyProgress;
        }

        public ZoneControlCaptureSpireSO SpireSO => _spireSO;
    }
}

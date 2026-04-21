using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCometController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCometSO _cometSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCometBlazed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _tailLabel;
        [SerializeField] private Text       _blazeLabel;
        [SerializeField] private Slider     _tailBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCometBlazedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCometBlazedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCometBlazed?.RegisterCallback(_handleCometBlazedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCometBlazed?.UnregisterCallback(_handleCometBlazedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cometSO == null) return;
            int bonus = _cometSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cometSO == null) return;
            _cometSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cometSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cometSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_tailLabel != null)
                _tailLabel.text = $"Tails: {_cometSO.Tails}/{_cometSO.TailsNeeded}";

            if (_blazeLabel != null)
                _blazeLabel.text = $"Blazes: {_cometSO.BlazeCount}";

            if (_tailBar != null)
                _tailBar.value = _cometSO.TailProgress;
        }

        public ZoneControlCaptureCometSO CometSO => _cometSO;
    }
}

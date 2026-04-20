using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureShrineController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureShrineSO _shrineSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onShrinePurified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _candleLabel;
        [SerializeField] private Text       _purificationLabel;
        [SerializeField] private Slider     _candleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleShrinePurifiedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate          = HandlePlayerCaptured;
            _handleBotDelegate             = HandleBotCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handleShrinePurifiedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onShrinePurified?.RegisterCallback(_handleShrinePurifiedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onShrinePurified?.UnregisterCallback(_handleShrinePurifiedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_shrineSO == null) return;
            int bonus = _shrineSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_shrineSO == null) return;
            _shrineSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _shrineSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_shrineSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_candleLabel != null)
                _candleLabel.text = $"Candles: {_shrineSO.Candles}/{_shrineSO.CandlesNeeded}";

            if (_purificationLabel != null)
                _purificationLabel.text = $"Purifications: {_shrineSO.PurificationCount}";

            if (_candleBar != null)
                _candleBar.value = _shrineSO.CandleProgress;
        }

        public ZoneControlCaptureShrineSO ShrineSO => _shrineSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBraidedController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBraidedSO _braidedSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBraided;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _braidLabel;
        [SerializeField] private Text       _braidCountLabel;
        [SerializeField] private Slider     _braidBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBraidedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleBraidedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBraided?.RegisterCallback(_handleBraidedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBraided?.UnregisterCallback(_handleBraidedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_braidedSO == null) return;
            int bonus = _braidedSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_braidedSO == null) return;
            _braidedSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _braidedSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_braidedSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_braidLabel != null)
                _braidLabel.text = $"Braids: {_braidedSO.Braids}/{_braidedSO.BraidsNeeded}";

            if (_braidCountLabel != null)
                _braidCountLabel.text = $"Braidings: {_braidedSO.BraidCount}";

            if (_braidBar != null)
                _braidBar.value = _braidedSO.BraidProgress;
        }

        public ZoneControlCaptureBraidedSO BraidedSO => _braidedSO;
    }
}

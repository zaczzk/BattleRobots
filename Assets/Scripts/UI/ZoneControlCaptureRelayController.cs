using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRelayController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRelaySO _relaySO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRelayTripped;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _closureLabel;
        [SerializeField] private Text       _tripLabel;
        [SerializeField] private Slider     _closureBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTrippedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleTrippedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRelayTripped?.RegisterCallback(_handleTrippedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRelayTripped?.UnregisterCallback(_handleTrippedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_relaySO == null) return;
            int bonus = _relaySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_relaySO == null) return;
            _relaySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _relaySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_relaySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_closureLabel != null)
                _closureLabel.text = $"Closures: {_relaySO.Closures}/{_relaySO.ClosuresNeeded}";

            if (_tripLabel != null)
                _tripLabel.text = $"Trips: {_relaySO.TripCount}";

            if (_closureBar != null)
                _closureBar.value = _relaySO.ClosureProgress;
        }

        public ZoneControlCaptureRelaySO RelaySO => _relaySO;
    }
}

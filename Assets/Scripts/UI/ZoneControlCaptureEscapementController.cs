using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureEscapementController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureEscapementSO _escapementSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onEscapementReleased;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _tickLabel;
        [SerializeField] private Text       _releaseLabel;
        [SerializeField] private Slider     _tickBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleReleasedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleReleasedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onEscapementReleased?.RegisterCallback(_handleReleasedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onEscapementReleased?.UnregisterCallback(_handleReleasedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_escapementSO == null) return;
            int bonus = _escapementSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_escapementSO == null) return;
            _escapementSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _escapementSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_escapementSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_tickLabel != null)
                _tickLabel.text = $"Ticks: {_escapementSO.Ticks}/{_escapementSO.TicksNeeded}";

            if (_releaseLabel != null)
                _releaseLabel.text = $"Releases: {_escapementSO.ReleaseCount}";

            if (_tickBar != null)
                _tickBar.value = _escapementSO.TickProgress;
        }

        public ZoneControlCaptureEscapementSO EscapementSO => _escapementSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureClockController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureClockSO _clockSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onClockRun;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _tickLabel;
        [SerializeField] private Text       _clockLabel;
        [SerializeField] private Slider     _tickBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleClockRunDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleClockRunDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onClockRun?.RegisterCallback(_handleClockRunDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onClockRun?.UnregisterCallback(_handleClockRunDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_clockSO == null) return;
            int bonus = _clockSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_clockSO == null) return;
            _clockSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _clockSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_clockSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_tickLabel != null)
                _tickLabel.text = $"Ticks: {_clockSO.Ticks}/{_clockSO.TicksNeeded}";

            if (_clockLabel != null)
                _clockLabel.text = $"Clock Runs: {_clockSO.ClockCount}";

            if (_tickBar != null)
                _tickBar.value = _clockSO.TickProgress;
        }

        public ZoneControlCaptureClockSO ClockSO => _clockSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlTimedBonusWindowController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlTimedBonusWindowSO _bonusWindowSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Min(0)]
        [SerializeField] private int _baseCaptureReward = 50;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onActivateBonusWindow;
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onWindowOpened;
        [SerializeField] private VoidGameEvent _onWindowClosed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _timerLabel;
        [SerializeField] private Slider     _progressBar;
        [SerializeField] private GameObject _panel;

        private Action _handleActivateDelegate;
        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleActivateDelegate      = HandleActivate;
            _handleZoneCapturedDelegate  = HandleZoneCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _refreshDelegate             = Refresh;
        }

        private void OnEnable()
        {
            _onActivateBonusWindow?.RegisterCallback(_handleActivateDelegate);
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onWindowOpened?.RegisterCallback(_refreshDelegate);
            _onWindowClosed?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onActivateBonusWindow?.UnregisterCallback(_handleActivateDelegate);
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onWindowOpened?.UnregisterCallback(_refreshDelegate);
            _onWindowClosed?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (_bonusWindowSO == null) return;
            _bonusWindowSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleActivate()
        {
            _bonusWindowSO?.OpenWindow();
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            if (_bonusWindowSO == null) return;
            int reward = _bonusWindowSO.ApplyMultiplier(_baseCaptureReward);
            _wallet?.AddFunds(reward);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _bonusWindowSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_bonusWindowSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _bonusWindowSO.IsActive ? "BONUS ACTIVE!" : "Standby";

            if (_timerLabel != null)
                _timerLabel.text = _bonusWindowSO.IsActive
                    ? $"{_bonusWindowSO.RemainingTime:F1}s"
                    : "--";

            if (_progressBar != null)
                _progressBar.value = _bonusWindowSO.WindowProgress;
        }

        public ZoneControlTimedBonusWindowSO BonusWindowSO => _bonusWindowSO;
    }
}

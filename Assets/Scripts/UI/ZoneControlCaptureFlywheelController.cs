using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFlywheelController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFlywheelSO _flywheelSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFlywheelRevolved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _turnLabel;
        [SerializeField] private Text       _revolutionLabel;
        [SerializeField] private Slider     _turnBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRevolvedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRevolvedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFlywheelRevolved?.RegisterCallback(_handleRevolvedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFlywheelRevolved?.UnregisterCallback(_handleRevolvedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_flywheelSO == null) return;
            int bonus = _flywheelSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_flywheelSO == null) return;
            _flywheelSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _flywheelSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_flywheelSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_turnLabel != null)
                _turnLabel.text = $"Turns: {_flywheelSO.Turns}/{_flywheelSO.TurnsNeeded}";

            if (_revolutionLabel != null)
                _revolutionLabel.text = $"Revolutions: {_flywheelSO.RevolutionCount}";

            if (_turnBar != null)
                _turnBar.value = _flywheelSO.TurnProgress;
        }

        public ZoneControlCaptureFlywheelSO FlywheelSO => _flywheelSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSolenoidController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSolenoidSO _solenoidSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSolenoidActuated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _plungerLabel;
        [SerializeField] private Text       _actuationLabel;
        [SerializeField] private Slider     _plungerBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleActuatedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleActuatedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSolenoidActuated?.RegisterCallback(_handleActuatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSolenoidActuated?.UnregisterCallback(_handleActuatedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_solenoidSO == null) return;
            int bonus = _solenoidSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_solenoidSO == null) return;
            _solenoidSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _solenoidSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_solenoidSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_plungerLabel != null)
                _plungerLabel.text = $"Plungers: {_solenoidSO.Plungers}/{_solenoidSO.PlungersNeeded}";

            if (_actuationLabel != null)
                _actuationLabel.text = $"Actuations: {_solenoidSO.ActuationCount}";

            if (_plungerBar != null)
                _plungerBar.value = _solenoidSO.PlungerProgress;
        }

        public ZoneControlCaptureSolenoidSO SolenoidSO => _solenoidSO;
    }
}

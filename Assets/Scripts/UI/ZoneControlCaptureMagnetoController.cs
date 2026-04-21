using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMagnetoController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMagnetoSO _magnetoSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMagnetoCharged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _poleLabel;
        [SerializeField] private Text       _pulseLabel;
        [SerializeField] private Slider     _poleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleChargedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleChargedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMagnetoCharged?.RegisterCallback(_handleChargedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMagnetoCharged?.UnregisterCallback(_handleChargedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_magnetoSO == null) return;
            int bonus = _magnetoSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_magnetoSO == null) return;
            _magnetoSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _magnetoSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_magnetoSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_poleLabel != null)
                _poleLabel.text = $"Poles: {_magnetoSO.Poles}/{_magnetoSO.PolesNeeded}";

            if (_pulseLabel != null)
                _pulseLabel.text = $"Pulses: {_magnetoSO.PulseCount}";

            if (_poleBar != null)
                _poleBar.value = _magnetoSO.PoleProgress;
        }

        public ZoneControlCaptureMagnetoSO MagnetoSO => _magnetoSO;
    }
}

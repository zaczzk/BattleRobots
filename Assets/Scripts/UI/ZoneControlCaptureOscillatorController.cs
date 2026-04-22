using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureOscillatorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureOscillatorSO _oscillatorSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onOscillatorCycled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _oscillationLabel;
        [SerializeField] private Text       _cycleLabel;
        [SerializeField] private Slider     _oscillationBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCycledDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCycledDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onOscillatorCycled?.RegisterCallback(_handleCycledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onOscillatorCycled?.UnregisterCallback(_handleCycledDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_oscillatorSO == null) return;
            int bonus = _oscillatorSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_oscillatorSO == null) return;
            _oscillatorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _oscillatorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_oscillatorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_oscillationLabel != null)
                _oscillationLabel.text = $"Oscillations: {_oscillatorSO.Oscillations}/{_oscillatorSO.OscillationsNeeded}";

            if (_cycleLabel != null)
                _cycleLabel.text = $"Cycles: {_oscillatorSO.CycleCount}";

            if (_oscillationBar != null)
                _oscillationBar.value = _oscillatorSO.OscillationProgress;
        }

        public ZoneControlCaptureOscillatorSO OscillatorSO => _oscillatorSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDecelerationController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDecelerationSO _decelerationSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onDecelerationPeak;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _decelLabel;
        [SerializeField] private Slider     _decelBar;
        [SerializeField] private GameObject _panel;

        private Action _handleBotCapturedDelegate;
        private Action _handlePlayerCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _handleDecelerationPeakDelegate;

        private void Awake()
        {
            _handleBotCapturedDelegate      = HandleBotCaptured;
            _handlePlayerCapturedDelegate   = HandlePlayerCaptured;
            _handleMatchStartedDelegate     = HandleMatchStarted;
            _handleMatchEndedDelegate       = HandleMatchEnded;
            _handleDecelerationPeakDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onDecelerationPeak?.RegisterCallback(_handleDecelerationPeakDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onDecelerationPeak?.UnregisterCallback(_handleDecelerationPeakDelegate);
        }

        private void Update()
        {
            if (_decelerationSO == null) return;
            _decelerationSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_decelerationSO == null) return;
            _decelerationSO.RecordBotCapture();
            Refresh();
        }

        private void HandlePlayerCaptured()
        {
            if (_decelerationSO == null) return;
            _decelerationSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _decelerationSO?.Reset();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            _decelerationSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_decelerationSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_decelLabel != null)
                _decelLabel.text = $"Decel: {_decelerationSO.CurrentDeceleration:F1}";

            if (_decelBar != null)
                _decelBar.value = _decelerationSO.DecelerationProgress;
        }

        public ZoneControlCaptureDecelerationSO DecelerationSO => _decelerationSO;
    }
}

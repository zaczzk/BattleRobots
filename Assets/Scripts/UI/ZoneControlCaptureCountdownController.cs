using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCountdownController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCountdownSO _countdownSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onSuccess;
        [SerializeField] private VoidGameEvent _onFailed;

        [Header("UI References (optional)")]
        [SerializeField] private Text     _progressLabel;
        [SerializeField] private Text     _timerLabel;
        [SerializeField] private Slider   _progressBar;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchStartedDelegate;
        private Action _handleZoneCapturedDelegate;
        private Action _handleSuccessDelegate;
        private Action _handleFailedDelegate;

        private void Awake()
        {
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleSuccessDelegate      = HandleSuccess;
            _handleFailedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onSuccess?.RegisterCallback(_handleSuccessDelegate);
            _onFailed?.RegisterCallback(_handleFailedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onSuccess?.UnregisterCallback(_handleSuccessDelegate);
            _onFailed?.UnregisterCallback(_handleFailedDelegate);
        }

        private void Update()
        {
            if (_countdownSO == null || !_countdownSO.IsActive || _countdownSO.IsResolved) return;
            _countdownSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _countdownSO?.StartCountdown();
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            if (_countdownSO == null) return;
            _countdownSO.RecordCapture();
            Refresh();
        }

        private void HandleSuccess()
        {
            if (_countdownSO == null) return;
            _wallet?.AddFunds(_countdownSO.BonusOnSuccess);
            Refresh();
        }

        public void Refresh()
        {
            if (_countdownSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_progressLabel != null)
                _progressLabel.text =
                    $"Captures: {_countdownSO.CaptureCount}/{_countdownSO.CaptureTarget}";

            if (_timerLabel != null)
            {
                string timerText;
                if (!_countdownSO.IsResolved)
                    timerText = $"Time: {_countdownSO.RemainingTime:F1}s";
                else
                    timerText = _countdownSO.Succeeded ? "Done!" : "Failed";
                _timerLabel.text = timerText;
            }

            if (_progressBar != null)
                _progressBar.value = _countdownSO.CountdownProgress;
        }

        public ZoneControlCaptureCountdownSO CountdownSO => _countdownSO;
    }
}

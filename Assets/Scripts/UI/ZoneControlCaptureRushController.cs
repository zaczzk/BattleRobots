using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives <see cref="ZoneControlCaptureRushSO"/> rush
    /// detection, awards wallet bonuses on rush completion, and displays rush
    /// status and counts.
    ///
    /// <c>_onZoneCaptured</c>: records a capture + Refresh.
    /// <c>_onMatchStarted</c>: resets the detector + Refresh.
    /// <c>_onRushCompleted</c>: credits wallet + Refresh.
    /// <see cref="Update"/> ticks the detector each frame (window pruning).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRushController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRushSO _rushSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRushCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _captureCountLabel;
        [SerializeField] private Text       _rushCountLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRushCompletedDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate  = HandleZoneCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleRushCompletedDelegate = HandleRushCompleted;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRushCompleted?.RegisterCallback(_handleRushCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRushCompleted?.UnregisterCallback(_handleRushCompletedDelegate);
        }

        private void Update()
        {
            _rushSO?.Tick(Time.time);
        }

        private void HandleZoneCaptured()
        {
            _rushSO?.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _rushSO?.Reset();
            Refresh();
        }

        private void HandleRushCompleted()
        {
            if (_rushSO != null && _wallet != null)
                _wallet.AddFunds(_rushSO.RushBonus);
            Refresh();
        }

        public void Refresh()
        {
            if (_rushSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _rushSO.IsRushing ? "RUSHING!" : "Building...";

            if (_captureCountLabel != null)
                _captureCountLabel.text = $"Captures: {_rushSO.CaptureCount}";

            if (_rushCountLabel != null)
                _rushCountLabel.text = $"Rushes: {_rushSO.TotalRushCount}";
        }

        public ZoneControlCaptureRushSO RushSO => _rushSO;
    }
}

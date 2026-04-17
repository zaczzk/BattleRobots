using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the momentum-tracker lifecycle and displays
    /// burst state and in-window capture count.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <c>_onZoneCaptured</c>: calls <c>RecordCapture(Time.time)</c> + Refresh.
    ///   <c>_onMatchStarted</c>: resets the tracker + Refresh.
    ///   <c>_onBurstDetected/_onBurstEnded</c>: refreshes label.
    ///   <see cref="Update"/> ticks the tracker each frame (window pruning).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMomentumTrackerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMomentumTrackerSO _momentumSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBurstDetected;
        [SerializeField] private VoidGameEvent _onBurstEnded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _captureLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBurstDetected?.RegisterCallback(_refreshDelegate);
            _onBurstEnded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBurstDetected?.UnregisterCallback(_refreshDelegate);
            _onBurstEnded?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            _momentumSO?.Tick(Time.time);
        }

        private void HandleZoneCaptured()
        {
            _momentumSO?.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _momentumSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_momentumSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _momentumSO.IsBurst ? "BURST!" : "Normal";

            if (_captureLabel != null)
                _captureLabel.text = $"Captures: {_momentumSO.CaptureCount}";
        }

        public ZoneControlMomentumTrackerSO MomentumSO => _momentumSO;
    }
}

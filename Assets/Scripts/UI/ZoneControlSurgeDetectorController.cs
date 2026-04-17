using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the surge-detector lifecycle and displays
    /// surge state and in-window capture count.
    ///
    /// <c>_onZoneCaptured</c>: calls <c>RecordCapture(Time.time)</c> + Refresh.
    /// <c>_onMatchStarted</c>: resets the detector + Refresh.
    /// <c>_onSurgeStarted/_onSurgeEnded</c>: refreshes label.
    /// <see cref="Update"/> ticks the detector each frame (window pruning).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlSurgeDetectorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlSurgeDetectorSO _surgeSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSurgeStarted;
        [SerializeField] private VoidGameEvent _onSurgeEnded;

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
            _onSurgeStarted?.RegisterCallback(_refreshDelegate);
            _onSurgeEnded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSurgeStarted?.UnregisterCallback(_refreshDelegate);
            _onSurgeEnded?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            _surgeSO?.Tick(Time.time);
        }

        private void HandleZoneCaptured()
        {
            _surgeSO?.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _surgeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_surgeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _surgeSO.IsSurging ? "SURGE ACTIVE!" : "No Surge";

            if (_captureLabel != null)
                _captureLabel.text = $"Captures: {_surgeSO.CaptureCount}";
        }

        public ZoneControlSurgeDetectorSO SurgeSO => _surgeSO;
    }
}

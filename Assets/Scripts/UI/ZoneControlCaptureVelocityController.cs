using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that bridges zone-capture events into
    /// <see cref="ZoneControlCaptureVelocitySO"/> and displays the current
    /// capture velocity with a FAST!/Slow/Normal status badge.
    ///
    /// <c>_onZoneCaptured</c>: records a capture + Refresh.
    /// <c>_onMatchStarted</c>: resets the SO + Refresh.
    /// <c>_onHighVelocity/_onLowVelocity</c>: Refresh.
    /// <c>Update</c>: ticks the SO each frame to prune stale timestamps.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureVelocityController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureVelocitySO _velocitySO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHighVelocity;
        [SerializeField] private VoidGameEvent _onLowVelocity;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _velocityLabel;
        [SerializeField] private Text       _statusLabel;
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
            _onHighVelocity?.RegisterCallback(_refreshDelegate);
            _onLowVelocity?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHighVelocity?.UnregisterCallback(_refreshDelegate);
            _onLowVelocity?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (_velocitySO == null) return;
            _velocitySO.Tick(Time.time);
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            _velocitySO?.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _velocitySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_velocitySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_velocityLabel != null)
                _velocityLabel.text = $"Vel: {_velocitySO.GetVelocity(Time.time):F2}/s";

            if (_statusLabel != null)
            {
                if (_velocitySO.IsHighVelocity)
                    _statusLabel.text = "FAST!";
                else if (_velocitySO.IsLowVelocity)
                    _statusLabel.text = "Slow";
                else
                    _statusLabel.text = "Normal";
            }
        }

        public ZoneControlCaptureVelocitySO VelocitySO => _velocitySO;
    }
}

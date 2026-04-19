using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureAccelerationController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureAccelerationSO _accelerationSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onMaxAcceleration;

        [Header("UI References (optional)")]
        [SerializeField] private Text     _accelLabel;
        [SerializeField] private Slider   _accelBar;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _handleMaxAccelerationDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate    = HandleZoneCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handleMatchEndedDelegate      = HandleMatchEnded;
            _handleMaxAccelerationDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onMaxAcceleration?.RegisterCallback(_handleMaxAccelerationDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onMaxAcceleration?.UnregisterCallback(_handleMaxAccelerationDelegate);
        }

        private void Update()
        {
            if (_accelerationSO == null) return;
            _accelerationSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            if (_accelerationSO == null) return;
            _accelerationSO.RecordCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _accelerationSO?.Reset();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            _accelerationSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_accelerationSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_accelLabel != null)
                _accelLabel.text = $"Accel: {_accelerationSO.CurrentAcceleration:F1}";

            if (_accelBar != null)
                _accelBar.value = _accelerationSO.AccelerationProgress;
        }

        public ZoneControlCaptureAccelerationSO AccelerationSO => _accelerationSO;
    }
}

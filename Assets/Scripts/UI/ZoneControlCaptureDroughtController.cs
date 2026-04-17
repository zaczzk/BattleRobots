using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDroughtController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDroughtSO _droughtSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onDroughtStarted;
        [SerializeField] private VoidGameEvent _onDroughtEnded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _timerLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchResetDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchResetDelegate   = HandleMatchReset;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchResetDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchResetDelegate);
            _onDroughtStarted?.RegisterCallback(_refreshDelegate);
            _onDroughtEnded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchResetDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchResetDelegate);
            _onDroughtStarted?.UnregisterCallback(_refreshDelegate);
            _onDroughtEnded?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (_droughtSO == null) return;
            _droughtSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            _droughtSO?.RecordCapture();
            Refresh();
        }

        private void HandleMatchReset()
        {
            _droughtSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_droughtSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _droughtSO.IsDrought ? "DROUGHT!" : "Active";

            if (_timerLabel != null)
                _timerLabel.text = $"No Capture: {_droughtSO.TimeSinceCapture:F1}s";
        }

        public ZoneControlCaptureDroughtSO DroughtSO => _droughtSO;
    }
}

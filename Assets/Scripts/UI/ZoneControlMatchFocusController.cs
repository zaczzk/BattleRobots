using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchFocusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchFocusSO _focusSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onFocusLost;
        [SerializeField] private VoidGameEvent _onFocusRegained;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _idleTimerLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _handleRefreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMatchEndedDelegate   = HandleMatchEnded;
            _handleRefreshDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onFocusLost?.RegisterCallback(_handleRefreshDelegate);
            _onFocusRegained?.RegisterCallback(_handleRefreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onFocusLost?.UnregisterCallback(_handleRefreshDelegate);
            _onFocusRegained?.UnregisterCallback(_handleRefreshDelegate);
        }

        private void Update()
        {
            _focusSO?.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            if (_focusSO == null) return;
            _focusSO.RecordCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _focusSO?.Reset();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            _focusSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_focusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _focusSO.IsFocusLost ? "Focus Lost" : "Active";

            if (_idleTimerLabel != null)
                _idleTimerLabel.text = $"Idle: {_focusSO.IdleTime:F1}s";
        }

        public ZoneControlMatchFocusSO FocusSO => _focusSO;
    }
}

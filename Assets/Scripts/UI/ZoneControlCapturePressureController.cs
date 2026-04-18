using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePressureController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePressureSO _pressureSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onHighPressure;
        [SerializeField] private VoidGameEvent _onPressureNormal;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _pressureLabel;
        [SerializeField] private Slider     _pressureBar;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchStartedDelegate;
        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate   = HandleBotCaptured;
            _refreshDelegate             = Refresh;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onHighPressure?.RegisterCallback(_refreshDelegate);
            _onPressureNormal?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onHighPressure?.UnregisterCallback(_refreshDelegate);
            _onPressureNormal?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (_pressureSO == null) return;
            _pressureSO.Tick(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _pressureSO?.Reset();
            Refresh();
        }

        private void HandlePlayerCaptured()
        {
            _pressureSO?.RecordPlayerCapture(Time.time);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            _pressureSO?.RecordBotCapture(Time.time);
            Refresh();
        }

        public void Refresh()
        {
            if (_pressureSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_pressureLabel != null)
                _pressureLabel.text = $"Pressure: {_pressureSO.PressureRatio * 100f:F0}%";

            if (_pressureBar != null)
                _pressureBar.value = _pressureSO.PressureRatio;
        }

        public ZoneControlCapturePressureSO PressureSO => _pressureSO;
    }
}

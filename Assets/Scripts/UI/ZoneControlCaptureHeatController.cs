using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureHeatController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureHeatSO _heatSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onHeatHigh;
        [SerializeField] private VoidGameEvent _onHeatCooled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _heatLabel;
        [SerializeField] private Slider     _heatBar;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _handleHeatHighDelegate;
        private Action _handleHeatCooledDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMatchEndedDelegate   = HandleMatchEnded;
            _handleHeatHighDelegate     = Refresh;
            _handleHeatCooledDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onHeatHigh?.RegisterCallback(_handleHeatHighDelegate);
            _onHeatCooled?.RegisterCallback(_handleHeatCooledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onHeatHigh?.UnregisterCallback(_handleHeatHighDelegate);
            _onHeatCooled?.UnregisterCallback(_handleHeatCooledDelegate);
        }

        private void Update()
        {
            if (_heatSO == null) return;
            _heatSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            _heatSO?.RecordCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _heatSO?.Reset();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            _heatSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_heatSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_heatLabel != null)
                _heatLabel.text = $"Heat: {_heatSO.CurrentHeat:F1}";

            if (_heatBar != null)
                _heatBar.value = _heatSO.HeatProgress;
        }

        public ZoneControlCaptureHeatSO HeatSO => _heatSO;
    }
}

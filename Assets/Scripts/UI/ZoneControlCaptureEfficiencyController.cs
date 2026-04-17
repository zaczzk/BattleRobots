using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureEfficiencyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureEfficiencySO _efficiencySO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHighEfficiency;
        [SerializeField] private VoidGameEvent _onLowEfficiency;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _efficiencyLabel;
        [SerializeField] private Slider     _efficiencyBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCaptureDelegate;
        private Action _handleBotCaptureDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handlePlayerCaptureDelegate = HandlePlayerCapture;
            _handleBotCaptureDelegate    = HandleBotCapture;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _refreshDelegate             = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCaptureDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCaptureDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHighEfficiency?.RegisterCallback(_refreshDelegate);
            _onLowEfficiency?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCaptureDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCaptureDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHighEfficiency?.UnregisterCallback(_refreshDelegate);
            _onLowEfficiency?.UnregisterCallback(_refreshDelegate);
        }

        private void HandlePlayerCapture()
        {
            _efficiencySO?.RecordPlayerCapture();
            Refresh();
        }

        private void HandleBotCapture()
        {
            _efficiencySO?.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _efficiencySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_efficiencySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_efficiencyLabel != null)
                _efficiencyLabel.text = $"Efficiency: {Mathf.RoundToInt(_efficiencySO.Efficiency * 100f)}%";

            if (_efficiencyBar != null)
                _efficiencyBar.value = _efficiencySO.Efficiency;
        }

        public ZoneControlCaptureEfficiencySO EfficiencySO => _efficiencySO;
    }
}

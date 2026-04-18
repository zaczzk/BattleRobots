using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneThreatLevelController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneThreatLevelSO _threatLevelSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onThreatChanged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _threatLabel;
        [SerializeField] private Slider     _threatBar;
        [SerializeField] private GameObject _panel;

        private Action _handleBotCapturedDelegate;
        private Action _handlePlayerCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _refreshDelegate              = Refresh;
        }

        private void OnEnable()
        {
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onThreatChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onThreatChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (_threatLevelSO == null) return;
            _threatLevelSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_threatLevelSO == null) return;
            _threatLevelSO.RecordBotCapture();
            Refresh();
        }

        private void HandlePlayerCaptured()
        {
            if (_threatLevelSO == null) return;
            _threatLevelSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _threatLevelSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_threatLevelSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_threatLabel != null)
                _threatLabel.text = $"Threat: {_threatLevelSO.GetThreatLabel()}";

            if (_threatBar != null)
                _threatBar.value = _threatLevelSO.ThreatProgress;
        }

        public ZoneControlZoneThreatLevelSO ThreatLevelSO => _threatLevelSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchMomentumController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchMomentumSO _momentumSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMomentumHigh;
        [SerializeField] private VoidGameEvent _onMomentumLow;
        [SerializeField] private VoidGameEvent _onMomentumNeutral;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _momentumLabel;
        [SerializeField] private Slider     _momentumBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _refreshDelegate              = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMomentumHigh?.RegisterCallback(_refreshDelegate);
            _onMomentumLow?.RegisterCallback(_refreshDelegate);
            _onMomentumNeutral?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMomentumHigh?.UnregisterCallback(_refreshDelegate);
            _onMomentumLow?.UnregisterCallback(_refreshDelegate);
            _onMomentumNeutral?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (_momentumSO == null) return;
            _momentumSO.Tick(Time.time);
            Refresh();
        }

        private void HandlePlayerCaptured()
        {
            if (_momentumSO == null) return;
            _momentumSO.RecordPlayerCapture(Time.time);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_momentumSO == null) return;
            _momentumSO.RecordBotCapture(Time.time);
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

            if (_momentumLabel != null)
            {
                string label = _momentumSO.IsHigh    ? "Positive" :
                               _momentumSO.IsLow     ? "Negative" : "Neutral";
                _momentumLabel.text = $"Momentum: {label}";
            }

            if (_momentumBar != null)
                _momentumBar.value = _momentumSO.Momentum;
        }

        public ZoneControlMatchMomentumSO MomentumSO => _momentumSO;
    }
}

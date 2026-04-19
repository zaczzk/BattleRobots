using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchEndRushController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchEndRushSO _rushSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onRushTriggered;
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onRushStarted;
        [SerializeField] private VoidGameEvent _onRushEnded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleRushTriggeredDelegate;
        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _handleRushStartedDelegate;
        private Action _handleRushEndedDelegate;

        private void Awake()
        {
            _handleRushTriggeredDelegate = HandleRushTriggered;
            _handleZoneCapturedDelegate  = HandleZoneCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleMatchEndedDelegate    = HandleMatchEnded;
            _handleRushStartedDelegate   = Refresh;
            _handleRushEndedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onRushTriggered?.RegisterCallback(_handleRushTriggeredDelegate);
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onRushStarted?.RegisterCallback(_handleRushStartedDelegate);
            _onRushEnded?.RegisterCallback(_handleRushEndedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onRushTriggered?.UnregisterCallback(_handleRushTriggeredDelegate);
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onRushStarted?.UnregisterCallback(_handleRushStartedDelegate);
            _onRushEnded?.UnregisterCallback(_handleRushEndedDelegate);
        }

        private void HandleRushTriggered()
        {
            _rushSO?.StartRush();
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            if (_rushSO == null) return;
            int bonus = _rushSO.RecordCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _rushSO?.Reset();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            _rushSO?.EndRush();
            Refresh();
        }

        public void Refresh()
        {
            if (_rushSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _rushSO.IsActive ? "Rush Active!" : "Rush Pending";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Rush Bonus: {_rushSO.TotalRushBonus}";
        }

        public ZoneControlMatchEndRushSO RushSO => _rushSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMomentumShiftController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMomentumShiftSO _momentumShiftSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMomentumShift;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _shiftsLabel;
        [SerializeField] private Text       _leaderLabel;
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
            _onMomentumShift?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMomentumShift?.UnregisterCallback(_refreshDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_momentumShiftSO == null) return;
            _momentumShiftSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_momentumShiftSO == null) return;
            _momentumShiftSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _momentumShiftSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_momentumShiftSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_shiftsLabel != null)
                _shiftsLabel.text = $"Shifts: {_momentumShiftSO.ShiftCount}";

            if (_leaderLabel != null)
            {
                string leader = _momentumShiftSO.IsTied
                    ? "Tied"
                    : _momentumShiftSO.IsPlayerLeading ? "Player" : "Bot";
                _leaderLabel.text = $"Leader: {leader}";
            }
        }

        public ZoneControlCaptureMomentumShiftSO MomentumShiftSO => _momentumShiftSO;
    }
}

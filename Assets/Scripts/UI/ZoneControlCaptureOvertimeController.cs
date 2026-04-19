using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureOvertimeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureOvertimeSO _overtimeSO;
        [SerializeField] private PlayerWalletSO               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onOvertimeTriggered;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onOvertimeResolved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _leadLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleOvertimeTriggeredDelegate;
        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleOvertimeTriggeredDelegate = HandleOvertimeTriggered;
            _handlePlayerCapturedDelegate    = HandlePlayerCaptured;
            _handleBotCapturedDelegate       = HandleBotCaptured;
            _handleMatchStartedDelegate      = HandleMatchStarted;
            _handleMatchEndedDelegate        = HandleMatchEnded;
            _refreshDelegate                 = Refresh;
        }

        private void OnEnable()
        {
            _onOvertimeTriggered?.RegisterCallback(_handleOvertimeTriggeredDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onOvertimeResolved?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onOvertimeTriggered?.UnregisterCallback(_handleOvertimeTriggeredDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onOvertimeResolved?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleOvertimeTriggered()
        {
            _overtimeSO?.StartOvertime();
            Refresh();
        }

        private void HandlePlayerCaptured()
        {
            if (_overtimeSO == null) return;
            _overtimeSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_overtimeSO == null) return;
            _overtimeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _overtimeSO?.Reset();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            if (_overtimeSO == null) return;
            int bonus = _overtimeSO.ResolveOvertime();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        public void Refresh()
        {
            if (_overtimeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _overtimeSO.IsActive ? "OT ACTIVE!" : "Standby";

            if (_leadLabel != null)
                _leadLabel.text = $"OT Lead: {_overtimeSO.OvertimeLead}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"OT Bonus: {_overtimeSO.OvertimeBonus}";
        }

        public ZoneControlCaptureOvertimeSO OvertimeSO => _overtimeSO;
    }
}

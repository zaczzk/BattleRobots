using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneOvertimeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneOvertimeSO _overtimeSO;

        [Header("Economy (optional)")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onOvertimeCapture;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _overtimeCountLabel;
        [SerializeField] private Text       _overtimeBonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleOvertimeCaptureDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate  = HandlePlayerCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handleOvertimeCaptureDelegate = HandleOvertimeCapture;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onOvertimeCapture?.RegisterCallback(_handleOvertimeCaptureDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onOvertimeCapture?.UnregisterCallback(_handleOvertimeCaptureDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_overtimeSO == null) return;
            int prev = _overtimeSO.OvertimeCount;
            _overtimeSO.RecordPlayerCapture();
            if (_overtimeSO.OvertimeCount > prev)
                _wallet?.AddFunds(_overtimeSO.BonusPerOvertime);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _overtimeSO?.Reset();
            Refresh();
        }

        private void HandleOvertimeCapture()
        {
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

            if (_overtimeCountLabel != null)
                _overtimeCountLabel.text = $"Overtimes: {_overtimeSO.OvertimeCount}";

            if (_overtimeBonusLabel != null)
                _overtimeBonusLabel.text = $"Overtime Bonus: {_overtimeSO.TotalBonusAwarded}";
        }

        public ZoneControlZoneOvertimeSO OvertimeSO => _overtimeSO;
    }
}

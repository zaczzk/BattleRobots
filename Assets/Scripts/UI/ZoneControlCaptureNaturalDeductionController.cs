using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureNaturalDeductionController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureNaturalDeductionSO _naturalDeductionSO;
        [SerializeField] private PlayerWallet                         _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNaturalDeductionCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _dischargeStepLabel;
        [SerializeField] private Text       _dischargeCountLabel;
        [SerializeField] private Slider     _dischargeStepBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompletedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCompletedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNaturalDeductionCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNaturalDeductionCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_naturalDeductionSO == null) return;
            int bonus = _naturalDeductionSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_naturalDeductionSO == null) return;
            _naturalDeductionSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _naturalDeductionSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_naturalDeductionSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_dischargeStepLabel != null)
                _dischargeStepLabel.text =
                    $"Discharge Steps: {_naturalDeductionSO.DischargeSteps}/{_naturalDeductionSO.DischargeStepsNeeded}";

            if (_dischargeCountLabel != null)
                _dischargeCountLabel.text = $"Discharges: {_naturalDeductionSO.DischargeCount}";

            if (_dischargeStepBar != null)
                _dischargeStepBar.value = _naturalDeductionSO.DischargeStepProgress;
        }

        public ZoneControlCaptureNaturalDeductionSO NaturalDeductionSO => _naturalDeductionSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureStreakMeterController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureStreakMeterSO _meterSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMeterFull;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _meterLabel;
        [SerializeField] private Slider     _meterBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMeterFullDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleMeterFullDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMeterFull?.RegisterCallback(_handleMeterFullDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMeterFull?.UnregisterCallback(_handleMeterFullDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_meterSO == null) return;
            int prevFills = _meterSO.FillCount;
            _meterSO.RecordPlayerCapture();
            if (_meterSO.FillCount > prevFills)
                _wallet?.AddFunds(_meterSO.BonusOnFill);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_meterSO == null) return;
            _meterSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _meterSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_meterSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_meterLabel != null)
                _meterLabel.text = $"Meter: {Mathf.RoundToInt(_meterSO.MeterProgress * 100f)}%";

            if (_meterBar != null)
                _meterBar.value = _meterSO.MeterProgress;
        }

        public ZoneControlCaptureStreakMeterSO MeterSO => _meterSO;
    }
}

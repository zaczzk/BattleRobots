using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSpeedBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSpeedBonusSO _speedBonusSO;

        [Header("Economy (optional)")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onSpeedBonusAwarded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _speedBonusLabel;
        [SerializeField] private Text       _timeToFirstLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchStartedDelegate;
        private Action _handleZoneCapturedDelegate;
        private Action _handleSpeedBonusDelegate;

        private void Awake()
        {
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleSpeedBonusDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onSpeedBonusAwarded?.RegisterCallback(_handleSpeedBonusDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onSpeedBonusAwarded?.UnregisterCallback(_handleSpeedBonusDelegate);
        }

        private void HandleMatchStarted()
        {
            _speedBonusSO?.StartMatch(Time.time);
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            if (_speedBonusSO == null) return;
            int bonus = _speedBonusSO.RecordFirstCapture(Time.time);
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        public void Refresh()
        {
            if (_speedBonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_speedBonusLabel != null)
                _speedBonusLabel.text = $"Speed Bonus: {_speedBonusSO.LastBonusAmount}";

            if (_timeToFirstLabel != null)
                _timeToFirstLabel.text = _speedBonusSO.HasAwarded
                    ? $"Time to First: {_speedBonusSO.TimeToFirstCapture:F1}s"
                    : "Time to First: --";
        }

        public ZoneControlCaptureSpeedBonusSO SpeedBonusSO => _speedBonusSO;
    }
}

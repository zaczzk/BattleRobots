using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTimingBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTimingBonusSO _timingBonusSO;
        [SerializeField] private PlayerWalletSO                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTimingBonus;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _timingLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTimingBonusDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleTimingBonusDelegate  = HandleTimingBonus;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTimingBonus?.RegisterCallback(_handleTimingBonusDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTimingBonus?.UnregisterCallback(_handleTimingBonusDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_timingBonusSO == null) return;
            _timingBonusSO.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _timingBonusSO?.Reset();
            Refresh();
        }

        private void HandleTimingBonus()
        {
            if (_timingBonusSO == null) return;
            _wallet?.AddFunds(_timingBonusSO.BonusPerOnTime);
            Refresh();
        }

        public void Refresh()
        {
            if (_timingBonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_timingLabel != null)
                _timingLabel.text = $"On Time: {_timingBonusSO.OnTimeCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Timing Bonus: {_timingBonusSO.TotalTimingBonus}";
        }

        public ZoneControlCaptureTimingBonusSO TimingBonusSO => _timingBonusSO;
    }
}

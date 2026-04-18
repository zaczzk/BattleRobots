using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneRushBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneRushBonusSO _rushBonusSO;

        [Header("Economy (optional)")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRushAchieved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _rushProgressLabel;
        [SerializeField] private Text       _rushBonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRushAchievedDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRushAchievedDelegate = HandleRushAchieved;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRushAchieved?.RegisterCallback(_handleRushAchievedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRushAchieved?.UnregisterCallback(_handleRushAchievedDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_rushBonusSO == null) return;
            _rushBonusSO.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _rushBonusSO?.Reset();
            Refresh();
        }

        private void HandleRushAchieved()
        {
            if (_rushBonusSO == null) return;
            _wallet?.AddFunds(_rushBonusSO.RushBonus);
            Refresh();
        }

        public void Refresh()
        {
            if (_rushBonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_rushProgressLabel != null)
                _rushProgressLabel.text = $"Rush: {_rushBonusSO.FastCaptureCount}/{_rushBonusSO.RushTargetCount}";

            if (_rushBonusLabel != null)
                _rushBonusLabel.text = $"Bonus: {_rushBonusSO.TotalBonusAwarded}";
        }

        public ZoneControlZoneRushBonusSO RushBonusSO => _rushBonusSO;
    }
}

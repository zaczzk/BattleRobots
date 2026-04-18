using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSurgeBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSurgeBonusSO _surgeBonusSO;

        [Header("Economy (optional)")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSurgeBonus;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _surgeProgressLabel;
        [SerializeField] private Text       _surgeBonusLabel;
        [SerializeField] private Slider     _surgeBar;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSurgeBonusDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSurgeBonusDelegate   = HandleSurgeBonus;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSurgeBonus?.RegisterCallback(_handleSurgeBonusDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSurgeBonus?.UnregisterCallback(_handleSurgeBonusDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_surgeBonusSO == null) return;
            _surgeBonusSO.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _surgeBonusSO?.Reset();
            Refresh();
        }

        private void HandleSurgeBonus()
        {
            if (_surgeBonusSO == null) return;
            _wallet?.AddFunds(_surgeBonusSO.BonusPerSurge);
            Refresh();
        }

        public void Refresh()
        {
            if (_surgeBonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_surgeProgressLabel != null)
                _surgeProgressLabel.text = $"Surge: {_surgeBonusSO.SurgeTotal}";

            if (_surgeBonusLabel != null)
                _surgeBonusLabel.text = $"Bonus: {_surgeBonusSO.TotalBonusAwarded}";

            if (_surgeBar != null)
                _surgeBar.value = _surgeBonusSO.SurgeProgress;
        }

        public ZoneControlCaptureSurgeBonusSO SurgeBonusSO => _surgeBonusSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneUnderdogController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneUnderdogSO _underdogSO;

        [Header("Economy (optional)")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onUnderdogBonus;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _underdogLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _statusBadge;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleUnderdogBonusDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleUnderdogBonusDelegate  = HandleUnderdogBonus;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onUnderdogBonus?.RegisterCallback(_handleUnderdogBonusDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onUnderdogBonus?.UnregisterCallback(_handleUnderdogBonusDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_underdogSO == null) return;
            int prevCount = _underdogSO.UnderdogCount;
            _underdogSO.RecordPlayerCapture();
            if (_underdogSO.UnderdogCount > prevCount)
                _wallet?.AddFunds(_underdogSO.BonusPerUnderdogCapture);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_underdogSO == null) return;
            _underdogSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _underdogSO?.Reset();
            Refresh();
        }

        private void HandleUnderdogBonus() => Refresh();

        public void Refresh()
        {
            if (_underdogSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_underdogLabel != null)
                _underdogLabel.text = $"Underdog Caps: {_underdogSO.UnderdogCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Bonus: {_underdogSO.TotalBonusAwarded}";

            _statusBadge?.SetActive(_underdogSO.IsUnderdog);
        }

        public ZoneControlZoneUnderdogSO UnderdogSO => _underdogSO;
    }
}

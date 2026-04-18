using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureReboundController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureReboundSO _reboundSO;

        [Header("Economy (optional)")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneLost;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRebound;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _reboundLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneLostDelegate;
        private Action _handlePlayerCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleReboundDelegate;

        private void Awake()
        {
            _handleZoneLostDelegate       = HandleZoneLost;
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleReboundDelegate        = HandleRebound;
        }

        private void OnEnable()
        {
            _onZoneLost?.RegisterCallback(_handleZoneLostDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRebound?.RegisterCallback(_handleReboundDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneLost?.UnregisterCallback(_handleZoneLostDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRebound?.UnregisterCallback(_handleReboundDelegate);
        }

        private void HandleZoneLost()
        {
            if (_reboundSO == null) return;
            _reboundSO.RecordZoneLost(Time.time);
            Refresh();
        }

        private void HandlePlayerCaptured()
        {
            if (_reboundSO == null) return;
            _reboundSO.RecordRecapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _reboundSO?.Reset();
            Refresh();
        }

        private void HandleRebound()
        {
            if (_reboundSO == null) return;
            _wallet?.AddFunds(_reboundSO.BonusPerRebound);
            Refresh();
        }

        public void Refresh()
        {
            if (_reboundSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_reboundLabel != null)
                _reboundLabel.text = $"Rebounds: {_reboundSO.ReboundCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Bonus: {_reboundSO.TotalBonusAwarded}";
        }

        public ZoneControlCaptureReboundSO ReboundSO => _reboundSO;
    }
}

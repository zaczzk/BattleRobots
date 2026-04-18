using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSpreeTrackerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSpreeTrackerSO _spreeTrackerSO;

        [Header("Economy (optional)")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSpreeStarted;
        [SerializeField] private VoidGameEvent _onSpreeEnded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _spreeLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSpreeStartedDelegate;
        private Action _handleSpreeEndedDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleSpreeStartedDelegate   = Refresh;
            _handleSpreeEndedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSpreeStarted?.RegisterCallback(_handleSpreeStartedDelegate);
            _onSpreeEnded?.RegisterCallback(_handleSpreeEndedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSpreeStarted?.UnregisterCallback(_handleSpreeStartedDelegate);
            _onSpreeEnded?.UnregisterCallback(_handleSpreeEndedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_spreeTrackerSO == null) return;
            int bonus = _spreeTrackerSO.RecordPlayerCapture(Time.time);
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_spreeTrackerSO == null) return;
            _spreeTrackerSO.BreakSpree();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _spreeTrackerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_spreeTrackerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_spreeLabel != null)
                _spreeLabel.text = $"Spree: {_spreeTrackerSO.CurrentStreak}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Bonus: {_spreeTrackerSO.TotalBonusAwarded}";
        }

        public ZoneControlCaptureSpreeTrackerSO SpreeTrackerSO => _spreeTrackerSO;
    }
}

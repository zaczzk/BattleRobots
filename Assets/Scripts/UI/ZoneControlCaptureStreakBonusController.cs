using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureStreakBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureStreakBonusSO _streakBonusSO;
        [SerializeField] private PlayerWallet                     _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onStreakBonus;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _streakLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchResetDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchResetDelegate   = HandleMatchReset;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchResetDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchResetDelegate);
            _onStreakBonus?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchResetDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchResetDelegate);
            _onStreakBonus?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_streakBonusSO == null) return;
            int bonus = _streakBonusSO.RecordCapture();
            _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleMatchReset()
        {
            _streakBonusSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_streakBonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_streakLabel != null)
                _streakLabel.text = $"Streak: {_streakBonusSO.CurrentStreak}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Bonus: +{_streakBonusSO.LastBonusAwarded}";
        }

        public ZoneControlCaptureStreakBonusSO StreakBonusSO => _streakBonusSO;
    }
}

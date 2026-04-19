using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlDominanceStreakController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlDominanceStreakSO _dominanceStreakSO;
        [SerializeField] private ZoneDominanceSO              _dominanceSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDominanceStreakBonus;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _streakLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchEndedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDominanceStreakBonusDelegate;

        private void Awake()
        {
            _handleMatchEndedDelegate           = HandleMatchEnded;
            _handleMatchStartedDelegate         = Refresh;
            _handleDominanceStreakBonusDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDominanceStreakBonus?.RegisterCallback(_handleDominanceStreakBonusDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDominanceStreakBonus?.UnregisterCallback(_handleDominanceStreakBonusDelegate);
        }

        private void HandleMatchEnded()
        {
            if (_dominanceStreakSO == null) return;
            bool hadDominance = _dominanceSO != null && _dominanceSO.HasDominance;
            int prevBonus = _dominanceStreakSO.TotalBonusAwarded;
            _dominanceStreakSO.RecordMatchEnd(hadDominance);
            int earned = _dominanceStreakSO.TotalBonusAwarded - prevBonus;
            if (earned > 0)
                _wallet?.AddFunds(earned);
            Refresh();
        }

        public void Refresh()
        {
            if (_dominanceStreakSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_streakLabel != null)
                _streakLabel.text = $"Dom Streak: {_dominanceStreakSO.ConsecutiveDominanceHolds}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Bonus: {_dominanceStreakSO.TotalBonusAwarded}";
        }

        public ZoneControlDominanceStreakSO DominanceStreakSO => _dominanceStreakSO;
    }
}

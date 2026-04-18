using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneDefenseController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneDefenseSO _defenseSO;
        [SerializeField] private ZoneDominanceSO          _dominanceSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDefenseBonus;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _holdsLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchEndedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleMatchEndedDelegate   = HandleMatchEnded;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDefenseBonus?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDefenseBonus?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleMatchEnded()
        {
            if (_defenseSO == null) return;
            bool majorityHeld = _dominanceSO != null && _dominanceSO.HasDominance;
            int  prevBonus    = _defenseSO.TotalBonusAwarded;
            _defenseSO.RecordMatchEnd(majorityHeld);
            int earned = _defenseSO.TotalBonusAwarded - prevBonus;
            if (earned > 0)
                _wallet?.AddFunds(earned);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_defenseSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_holdsLabel != null)
                _holdsLabel.text = $"Holds: {_defenseSO.ConsecutiveHolds}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Defense Bonus: {_defenseSO.TotalBonusAwarded}";
        }

        public ZoneControlZoneDefenseSO DefenseSO => _defenseSO;
    }
}

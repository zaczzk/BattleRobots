using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneOwnershipBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneOwnershipBonusSO     _bonusSO;
        [SerializeField] private ZoneControlZoneControllerCatalogSO  _catalogSO;
        [SerializeField] private PlayerWallet                         _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onOwnershipBonusAwarded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _ownedLabel;
        [SerializeField] private Text       _totalLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _handleBonusAwardedDelegate;

        private int _totalBonusEarned;

        private void Awake()
        {
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMatchEndedDelegate   = HandleMatchEnded;
            _handleBonusAwardedDelegate = HandleBonusAwarded;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onOwnershipBonusAwarded?.RegisterCallback(_handleBonusAwardedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onOwnershipBonusAwarded?.UnregisterCallback(_handleBonusAwardedDelegate);
        }

        private void Update()
        {
            if (_bonusSO == null) return;
            _bonusSO.Tick(Time.deltaTime);
        }

        private void HandleMatchStarted()
        {
            _totalBonusEarned = 0;
            _bonusSO?.Reset();
            _bonusSO?.StartTracking();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            _bonusSO?.StopTracking();
            Refresh();
        }

        private void HandleBonusAwarded()
        {
            if (_bonusSO == null) return;
            int zonesOwned = _catalogSO?.PlayerOwnedCount ?? 0;
            int bonus      = _bonusSO.ComputeBonus(zonesOwned);
            _totalBonusEarned += bonus;
            _wallet?.AddFunds(bonus);
            Refresh();
        }

        public void Refresh()
        {
            if (_bonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            int zonesOwned = _catalogSO?.PlayerOwnedCount ?? 0;

            if (_ownedLabel != null)
                _ownedLabel.text = $"Owned: {zonesOwned}";

            if (_totalLabel != null)
                _totalLabel.text = $"Total Bonus: {_totalBonusEarned}";
        }

        public ZoneControlZoneOwnershipBonusSO BonusSO => _bonusSO;
    }
}

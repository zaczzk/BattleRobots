using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that tracks how many zones the player holds via
    /// <see cref="ZoneControlZoneControllerCatalogSO"/>, applies a multi-hold
    /// score bonus at match end via <see cref="ZoneControlZoneScoreBonusSO"/>,
    /// and displays bonus amounts.
    ///
    /// <c>_onControlChanged</c>: syncs zones held from the catalog + Refresh.
    /// <c>_onMatchEnded</c>: applies bonus, credits wallet + Refresh.
    /// <c>_onMatchStarted</c>: resets the bonus SO + Refresh.
    /// <c>_onBonusTriggered</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneScoreBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneScoreBonusSO         _bonusSO;
        [SerializeField] private ZoneControlZoneControllerCatalogSO  _catalogSO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onControlChanged;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBonusTriggered;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private Text       _totalLabel;
        [SerializeField] private Text       _zonesLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleControlChangedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleControlChangedDelegate = HandleControlChanged;
            _handleMatchEndedDelegate     = HandleMatchEnded;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _refreshDelegate              = Refresh;
        }

        private void OnEnable()
        {
            _onControlChanged?.RegisterCallback(_handleControlChangedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBonusTriggered?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onControlChanged?.UnregisterCallback(_handleControlChangedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBonusTriggered?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleControlChanged()
        {
            if (_bonusSO != null)
                _bonusSO.SetZonesHeld(_catalogSO != null ? _catalogSO.PlayerOwnedCount : 0);
            Refresh();
        }

        private void HandleMatchEnded()
        {
            if (_bonusSO == null) { Refresh(); return; }
            _bonusSO.ApplyBonus();
            if (_wallet != null && _bonusSO.LastBonusAmount > 0)
                _wallet.AddFunds(_bonusSO.LastBonusAmount);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _bonusSO?.Reset();
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

            if (_bonusLabel != null)
                _bonusLabel.text = $"Bonus: {_bonusSO.LastBonusAmount}";

            if (_totalLabel != null)
                _totalLabel.text = $"Total Bonus: {_bonusSO.TotalBonusAwarded}";

            if (_zonesLabel != null)
                _zonesLabel.text = $"Zones Held: {_bonusSO.ZonesHeld}";
        }

        public ZoneControlZoneScoreBonusSO BonusSO => _bonusSO;
    }
}

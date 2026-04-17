using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that applies a post-match endgame bonus to the player's
    /// wallet based on zones owned at match end, and displays the bonus amounts.
    ///
    /// <c>_onMatchEnded</c>: applies bonus from <c>_catalogSO.PlayerOwnedCount</c>
    ///   → credits wallet → Refresh.
    /// <c>_onMatchStarted</c>: resets the SO + Refresh.
    /// <c>_onBonusApplied</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlEndgameBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlEndgameBonusSO         _bonusSO;
        [SerializeField] private ZoneControlZoneControllerCatalogSO _catalogSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBonusApplied;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private Text       _totalLabel;
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
            _onBonusApplied?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBonusApplied?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleMatchEnded()
        {
            if (_bonusSO == null)
            {
                Refresh();
                return;
            }

            int zonesOwned = _catalogSO?.PlayerOwnedCount ?? 0;
            _bonusSO.ApplyBonus(zonesOwned);

            if (_bonusSO.LastBonusAmount > 0 && _wallet != null)
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
                _bonusLabel.text = $"Last Bonus: {_bonusSO.LastBonusAmount}";

            if (_totalLabel != null)
                _totalLabel.text = $"Total: {_bonusSO.TotalBonusAwarded}";
        }

        public ZoneControlEndgameBonusSO BonusSO => _bonusSO;
    }
}

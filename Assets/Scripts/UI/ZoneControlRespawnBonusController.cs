using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that bridges respawn events into
    /// <see cref="ZoneControlRespawnBonusSO"/>, awards the respawn bonus to the
    /// player's wallet on each respawn, and displays the respawn count and total bonus.
    ///
    /// <c>_onPlayerRespawned</c>: records a respawn, awards wallet bonus, Refresh.
    /// <c>_onMatchStarted</c>: resets the SO + Refresh.
    /// <c>_onRespawnBonusAwarded</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlRespawnBonusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlRespawnBonusSO _respawnBonusSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerRespawned;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRespawnBonusAwarded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _countLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerRespawnedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handlePlayerRespawnedDelegate = HandlePlayerRespawned;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _refreshDelegate               = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerRespawned?.RegisterCallback(_handlePlayerRespawnedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRespawnBonusAwarded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerRespawned?.UnregisterCallback(_handlePlayerRespawnedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRespawnBonusAwarded?.UnregisterCallback(_refreshDelegate);
        }

        private void HandlePlayerRespawned()
        {
            _respawnBonusSO?.RecordRespawn();
            if (_wallet != null && _respawnBonusSO != null)
                _wallet.AddFunds(_respawnBonusSO.BonusPerRespawn);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _respawnBonusSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_respawnBonusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_countLabel != null)
                _countLabel.text = $"Respawns: {_respawnBonusSO.RespawnCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Bonus: {_respawnBonusSO.TotalBonusAwarded}";
        }

        public ZoneControlRespawnBonusSO RespawnBonusSO => _respawnBonusSO;
    }
}

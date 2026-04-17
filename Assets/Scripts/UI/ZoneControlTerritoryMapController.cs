using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that bridges zone capture/loss events into
    /// <see cref="ZoneControlTerritoryMapSO"/> and renders a per-zone ownership grid.
    ///
    /// <c>_onZoneCaptured</c> (IntGameEvent): marks the zone index as player-owned + Refresh.
    /// <c>_onZoneLost</c> (IntGameEvent): marks the zone index as not player-owned + Refresh.
    /// <c>_onMatchStarted</c>: resets the SO + Refresh.
    /// <c>_onOwnershipChanged</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlTerritoryMapController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlTerritoryMapSO _territoryMapSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private IntGameEvent  _onZoneCaptured;
        [SerializeField] private IntGameEvent  _onZoneLost;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onOwnershipChanged;

        [Header("UI References (optional)")]
        [SerializeField] private GameObject[] _zoneBadges;
        [SerializeField] private Text         _countLabel;
        [SerializeField] private GameObject   _panel;

        private Action<int> _handleZoneCapturedDelegate;
        private Action<int> _handleZoneLostDelegate;
        private Action      _handleMatchStartedDelegate;
        private Action      _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleZoneLostDelegate     = HandleZoneLost;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onZoneLost?.RegisterCallback(_handleZoneLostDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onOwnershipChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onZoneLost?.UnregisterCallback(_handleZoneLostDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onOwnershipChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneCaptured(int zoneIndex)
        {
            _territoryMapSO?.SetPlayerOwned(zoneIndex, true);
            Refresh();
        }

        private void HandleZoneLost(int zoneIndex)
        {
            _territoryMapSO?.SetPlayerOwned(zoneIndex, false);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _territoryMapSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_territoryMapSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_zoneBadges != null)
            {
                for (int i = 0; i < _zoneBadges.Length; i++)
                {
                    if (_zoneBadges[i] == null) continue;
                    _zoneBadges[i].SetActive(_territoryMapSO.IsPlayerOwned(i));
                }
            }

            if (_countLabel != null)
                _countLabel.text = $"Owned: {_territoryMapSO.PlayerOwnedCount}/{_territoryMapSO.ZoneCount}";
        }

        public ZoneControlTerritoryMapSO TerritoryMapSO => _territoryMapSO;
    }
}

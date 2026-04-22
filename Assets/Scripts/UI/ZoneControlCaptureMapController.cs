using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMapController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMapSO _mapSO;
        [SerializeField] private PlayerWallet            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMapBuilt;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _mappingLabel;
        [SerializeField] private Text       _mapLabel;
        [SerializeField] private Slider     _mappingBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMapBuiltDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMapBuiltDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMapBuilt?.RegisterCallback(_handleMapBuiltDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMapBuilt?.UnregisterCallback(_handleMapBuiltDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_mapSO == null) return;
            int bonus = _mapSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_mapSO == null) return;
            _mapSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _mapSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_mapSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_mappingLabel != null)
                _mappingLabel.text = $"Mappings: {_mapSO.Mappings}/{_mapSO.MappingsNeeded}";

            if (_mapLabel != null)
                _mapLabel.text = $"Maps: {_mapSO.MapCount}";

            if (_mappingBar != null)
                _mappingBar.value = _mapSO.MappingProgress;
        }

        public ZoneControlCaptureMapSO MapSO => _mapSO;
    }
}

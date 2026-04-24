using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMappingConeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMappingConeSO _mappingConeSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMappingConeConed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _chainMapLabel;
        [SerializeField] private Text       _coneLabel;
        [SerializeField] private Slider     _chainMapBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleConedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleConedDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMappingConeConed?.RegisterCallback(_handleConedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMappingConeConed?.UnregisterCallback(_handleConedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_mappingConeSO == null) return;
            int bonus = _mappingConeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_mappingConeSO == null) return;
            _mappingConeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _mappingConeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_mappingConeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_chainMapLabel != null)
                _chainMapLabel.text = $"Chain Maps: {_mappingConeSO.ChainMaps}/{_mappingConeSO.ChainMapsNeeded}";

            if (_coneLabel != null)
                _coneLabel.text = $"Cones: {_mappingConeSO.ConeCount}";

            if (_chainMapBar != null)
                _chainMapBar.value = _mappingConeSO.ChainMapProgress;
        }

        public ZoneControlCaptureMappingConeSO MappingConeSO => _mappingConeSO;
    }
}

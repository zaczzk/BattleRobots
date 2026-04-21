using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTrowelController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTrowelSO _trowelSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTrowelSet;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _layerLabel;
        [SerializeField] private Text       _setLabel;
        [SerializeField] private Slider     _layerBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSetDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSetDelegate          = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTrowelSet?.RegisterCallback(_handleSetDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTrowelSet?.UnregisterCallback(_handleSetDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_trowelSO == null) return;
            int bonus = _trowelSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_trowelSO == null) return;
            _trowelSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _trowelSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_trowelSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_layerLabel != null)
                _layerLabel.text = $"Layers: {_trowelSO.Layers}/{_trowelSO.LayersNeeded}";

            if (_setLabel != null)
                _setLabel.text = $"Sets: {_trowelSO.SetCount}";

            if (_layerBar != null)
                _layerBar.value = _trowelSO.LayerProgress;
        }

        public ZoneControlCaptureTrowelSO TrowelSO => _trowelSO;
    }
}

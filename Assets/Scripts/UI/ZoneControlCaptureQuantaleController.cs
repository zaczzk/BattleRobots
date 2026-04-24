using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureQuantaleController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureQuantaleSO _quantaleSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onQuantaleComposed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _compositeLabel;
        [SerializeField] private Text       _composeCountLabel;
        [SerializeField] private Slider     _compositeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleQuantaleComposedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate           = HandlePlayerCaptured;
            _handleBotDelegate              = HandleBotCaptured;
            _handleMatchStartedDelegate     = HandleMatchStarted;
            _handleQuantaleComposedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onQuantaleComposed?.RegisterCallback(_handleQuantaleComposedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onQuantaleComposed?.UnregisterCallback(_handleQuantaleComposedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_quantaleSO == null) return;
            int bonus = _quantaleSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_quantaleSO == null) return;
            _quantaleSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _quantaleSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_quantaleSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_compositeLabel != null)
                _compositeLabel.text = $"Composites: {_quantaleSO.Composites}/{_quantaleSO.CompositesNeeded}";

            if (_composeCountLabel != null)
                _composeCountLabel.text = $"Compositions: {_quantaleSO.ComposeCount}";

            if (_compositeBar != null)
                _compositeBar.value = _quantaleSO.CompositeProgress;
        }

        public ZoneControlCaptureQuantaleSO QuantaleSO => _quantaleSO;
    }
}

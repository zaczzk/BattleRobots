using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureComposeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureComposeSO _composeSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onComposeComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _stepLabel;
        [SerializeField] private Text       _composeLabel;
        [SerializeField] private Slider     _stepBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleComposeCompleteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate         = HandlePlayerCaptured;
            _handleBotDelegate            = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleComposeCompleteDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onComposeComplete?.RegisterCallback(_handleComposeCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onComposeComplete?.UnregisterCallback(_handleComposeCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_composeSO == null) return;
            int bonus = _composeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_composeSO == null) return;
            _composeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _composeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_composeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_stepLabel != null)
                _stepLabel.text = $"Steps: {_composeSO.Steps}/{_composeSO.StepsNeeded}";

            if (_composeLabel != null)
                _composeLabel.text = $"Composes: {_composeSO.ComposeCount}";

            if (_stepBar != null)
                _stepBar.value = _composeSO.ComposeProgress;
        }

        public ZoneControlCaptureComposeSO ComposeSO => _composeSO;
    }
}

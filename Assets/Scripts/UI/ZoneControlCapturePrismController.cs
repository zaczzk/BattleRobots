using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePrismController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePrismSO _prismSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPrismRefracted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _fragmentLabel;
        [SerializeField] private Text       _refractionLabel;
        [SerializeField] private Slider     _fragmentBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePrismRefractedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handlePrismRefractedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPrismRefracted?.RegisterCallback(_handlePrismRefractedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPrismRefracted?.UnregisterCallback(_handlePrismRefractedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_prismSO == null) return;
            int bonus = _prismSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_prismSO == null) return;
            _prismSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _prismSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_prismSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_fragmentLabel != null)
                _fragmentLabel.text = $"Fragments: {_prismSO.Fragments}/{_prismSO.FragmentsNeeded}";

            if (_refractionLabel != null)
                _refractionLabel.text = $"Refractions: {_prismSO.RefractionCount}";

            if (_fragmentBar != null)
                _fragmentBar.value = _prismSO.FragmentProgress;
        }

        public ZoneControlCapturePrismSO PrismSO => _prismSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureYonedaController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureYonedaSO _yonedaSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onYonedaEmbedded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _embedLabel;
        [SerializeField] private Text       _embedCountLabel;
        [SerializeField] private Slider     _embedBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleEmbeddedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleEmbeddedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onYonedaEmbedded?.RegisterCallback(_handleEmbeddedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onYonedaEmbedded?.UnregisterCallback(_handleEmbeddedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_yonedaSO == null) return;
            int bonus = _yonedaSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_yonedaSO == null) return;
            _yonedaSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _yonedaSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_yonedaSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_embedLabel != null)
                _embedLabel.text = $"Embeds: {_yonedaSO.Embeds}/{_yonedaSO.EmbedsNeeded}";

            if (_embedCountLabel != null)
                _embedCountLabel.text = $"Embeddings: {_yonedaSO.EmbedCount}";

            if (_embedBar != null)
                _embedBar.value = _yonedaSO.EmbedProgress;
        }

        public ZoneControlCaptureYonedaSO YonedaSO => _yonedaSO;
    }
}

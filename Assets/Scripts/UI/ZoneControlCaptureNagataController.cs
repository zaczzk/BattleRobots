using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureNagataController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureNagataSO _nagataSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNagataCompactified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _embeddingLabel;
        [SerializeField] private Text       _compactificationLabel;
        [SerializeField] private Slider     _embeddingBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompactifiedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleCompactifiedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNagataCompactified?.RegisterCallback(_handleCompactifiedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNagataCompactified?.UnregisterCallback(_handleCompactifiedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_nagataSO == null) return;
            int bonus = _nagataSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_nagataSO == null) return;
            _nagataSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _nagataSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_nagataSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_embeddingLabel != null)
                _embeddingLabel.text = $"Embeddings: {_nagataSO.Embeddings}/{_nagataSO.EmbeddingsNeeded}";

            if (_compactificationLabel != null)
                _compactificationLabel.text = $"Compactifications: {_nagataSO.CompactificationCount}";

            if (_embeddingBar != null)
                _embeddingBar.value = _nagataSO.EmbeddingProgress;
        }

        public ZoneControlCaptureNagataSO NagataSO => _nagataSO;
    }
}

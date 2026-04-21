using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTransformerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTransformerSO _transformerSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTransformerInduced;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _turnLabel;
        [SerializeField] private Text       _inductionLabel;
        [SerializeField] private Slider     _turnBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleInducedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleInducedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTransformerInduced?.RegisterCallback(_handleInducedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTransformerInduced?.UnregisterCallback(_handleInducedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_transformerSO == null) return;
            int bonus = _transformerSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_transformerSO == null) return;
            _transformerSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _transformerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_transformerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_turnLabel != null)
                _turnLabel.text = $"Turns: {_transformerSO.Turns}/{_transformerSO.TurnsNeeded}";

            if (_inductionLabel != null)
                _inductionLabel.text = $"Inductions: {_transformerSO.InductionCount}";

            if (_turnBar != null)
                _turnBar.value = _transformerSO.TurnProgress;
        }

        public ZoneControlCaptureTransformerSO TransformerSO => _transformerSO;
    }
}

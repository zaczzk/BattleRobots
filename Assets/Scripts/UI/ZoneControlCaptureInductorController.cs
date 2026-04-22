using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureInductorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureInductorSO _inductorSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onInductorCharged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _turnLabel;
        [SerializeField] private Text       _inductionLabel;
        [SerializeField] private Slider     _turnBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleChargedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleChargedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onInductorCharged?.RegisterCallback(_handleChargedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onInductorCharged?.UnregisterCallback(_handleChargedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_inductorSO == null) return;
            int bonus = _inductorSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_inductorSO == null) return;
            _inductorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _inductorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_inductorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_turnLabel != null)
                _turnLabel.text = $"Turns: {_inductorSO.Turns}/{_inductorSO.TurnsNeeded}";

            if (_inductionLabel != null)
                _inductionLabel.text = $"Inductions: {_inductorSO.InductionCount}";

            if (_turnBar != null)
                _turnBar.value = _inductorSO.TurnProgress;
        }

        public ZoneControlCaptureInductorSO InductorSO => _inductorSO;
    }
}

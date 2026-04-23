using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureContravariantController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureContravariantSO _contravariantSO;
        [SerializeField] private PlayerWallet                      _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onContramapped;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _contraLabel;
        [SerializeField] private Text       _contramapLabel;
        [SerializeField] private Slider     _contraBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleContramappedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleContramappedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onContramapped?.RegisterCallback(_handleContramappedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onContramapped?.UnregisterCallback(_handleContramappedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_contravariantSO == null) return;
            int bonus = _contravariantSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_contravariantSO == null) return;
            _contravariantSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _contravariantSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_contravariantSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_contraLabel != null)
                _contraLabel.text = $"Contras: {_contravariantSO.Contras}/{_contravariantSO.ContrasNeeded}";

            if (_contramapLabel != null)
                _contramapLabel.text = $"Contramaps: {_contravariantSO.ContramapCount}";

            if (_contraBar != null)
                _contraBar.value = _contravariantSO.ContraProgress;
        }

        public ZoneControlCaptureContravariantSO ContravariantSO => _contravariantSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureHomotopyTypeTheoryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureHomotopyTypeTheorySO _homotopyTypeTheorySO;
        [SerializeField] private PlayerWallet                           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHomotopyTypeTheoryCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _pathEquivalenceLabel;
        [SerializeField] private Text       _univalenceCountLabel;
        [SerializeField] private Slider     _pathEquivalenceBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompletedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCompletedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHomotopyTypeTheoryCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHomotopyTypeTheoryCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_homotopyTypeTheorySO == null) return;
            int bonus = _homotopyTypeTheorySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_homotopyTypeTheorySO == null) return;
            _homotopyTypeTheorySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _homotopyTypeTheorySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_homotopyTypeTheorySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_pathEquivalenceLabel != null)
                _pathEquivalenceLabel.text =
                    $"Path Equivalences: {_homotopyTypeTheorySO.PathEquivalences}/{_homotopyTypeTheorySO.PathEquivalencesNeeded}";

            if (_univalenceCountLabel != null)
                _univalenceCountLabel.text = $"Univalences: {_homotopyTypeTheorySO.UnivalenceCount}";

            if (_pathEquivalenceBar != null)
                _pathEquivalenceBar.value = _homotopyTypeTheorySO.PathEquivalenceProgress;
        }

        public ZoneControlCaptureHomotopyTypeTheorySO HomotopyTypeTheorySO => _homotopyTypeTheorySO;
    }
}

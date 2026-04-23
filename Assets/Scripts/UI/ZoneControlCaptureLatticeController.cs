using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLatticeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLatticeSO _latticeSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onJoinFormed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _joinLabel;
        [SerializeField] private Text       _joinCountLabel;
        [SerializeField] private Slider     _joinBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleJoinFormedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleJoinFormedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onJoinFormed?.RegisterCallback(_handleJoinFormedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onJoinFormed?.UnregisterCallback(_handleJoinFormedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_latticeSO == null) return;
            int bonus = _latticeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_latticeSO == null) return;
            _latticeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _latticeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_latticeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_joinLabel != null)
                _joinLabel.text = $"Joins: {_latticeSO.Joins}/{_latticeSO.JoinsNeeded}";

            if (_joinCountLabel != null)
                _joinCountLabel.text = $"Lattice Joins: {_latticeSO.JoinCount}";

            if (_joinBar != null)
                _joinBar.value = _latticeSO.JoinProgress;
        }

        public ZoneControlCaptureLatticeSO LatticeSO => _latticeSO;
    }
}

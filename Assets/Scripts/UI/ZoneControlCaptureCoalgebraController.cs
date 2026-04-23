using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCoalgebraController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCoalgebraSO _coalgebraSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCoalgebraUnfolded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _stateLabel;
        [SerializeField] private Text       _unfoldLabel;
        [SerializeField] private Slider     _stateBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleUnfoldedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleUnfoldedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCoalgebraUnfolded?.RegisterCallback(_handleUnfoldedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCoalgebraUnfolded?.UnregisterCallback(_handleUnfoldedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_coalgebraSO == null) return;
            int bonus = _coalgebraSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_coalgebraSO == null) return;
            _coalgebraSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _coalgebraSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_coalgebraSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_stateLabel != null)
                _stateLabel.text = $"States: {_coalgebraSO.States}/{_coalgebraSO.StatesNeeded}";

            if (_unfoldLabel != null)
                _unfoldLabel.text = $"Unfolds: {_coalgebraSO.UnfoldCount}";

            if (_stateBar != null)
                _stateBar.value = _coalgebraSO.StateProgress;
        }

        public ZoneControlCaptureCoalgebraSO CoalgebraSO => _coalgebraSO;
    }
}

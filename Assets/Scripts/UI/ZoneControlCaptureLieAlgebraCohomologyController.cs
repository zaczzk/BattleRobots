using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLieAlgebraCohomologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLieAlgebraCohomologySO _lieAlgebraSO;
        [SerializeField] private PlayerWallet                             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLieAlgebraCohomologyReduced;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _chainLabel;
        [SerializeField] private Text       _reduceLabel;
        [SerializeField] private Slider     _chainBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleReducedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleReducedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLieAlgebraCohomologyReduced?.RegisterCallback(_handleReducedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLieAlgebraCohomologyReduced?.UnregisterCallback(_handleReducedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_lieAlgebraSO == null) return;
            int bonus = _lieAlgebraSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_lieAlgebraSO == null) return;
            _lieAlgebraSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _lieAlgebraSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_lieAlgebraSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_chainLabel != null)
                _chainLabel.text = $"Chains: {_lieAlgebraSO.Chains}/{_lieAlgebraSO.ChainsNeeded}";

            if (_reduceLabel != null)
                _reduceLabel.text = $"Reductions: {_lieAlgebraSO.ReduceCount}";

            if (_chainBar != null)
                _chainBar.value = _lieAlgebraSO.ChainProgress;
        }

        public ZoneControlCaptureLieAlgebraCohomologySO LieAlgebraSO => _lieAlgebraSO;
    }
}

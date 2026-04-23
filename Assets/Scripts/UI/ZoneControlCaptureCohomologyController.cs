using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCohomologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCohomologySO _cohomologySO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCohomologyComputed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _cocycleLabel;
        [SerializeField] private Text       _computeLabel;
        [SerializeField] private Slider     _cocycleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleComputedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleComputedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCohomologyComputed?.RegisterCallback(_handleComputedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCohomologyComputed?.UnregisterCallback(_handleComputedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cohomologySO == null) return;
            int bonus = _cohomologySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cohomologySO == null) return;
            _cohomologySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cohomologySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cohomologySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_cocycleLabel != null)
                _cocycleLabel.text = $"Cocycles: {_cohomologySO.Cocycles}/{_cohomologySO.CocyclesNeeded}";

            if (_computeLabel != null)
                _computeLabel.text = $"Computations: {_cohomologySO.ComputationCount}";

            if (_cocycleBar != null)
                _cocycleBar.value = _cohomologySO.CocycleProgress;
        }

        public ZoneControlCaptureCohomologySO CohomologySO => _cohomologySO;
    }
}

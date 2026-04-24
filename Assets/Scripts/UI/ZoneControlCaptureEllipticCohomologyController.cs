using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureEllipticCohomologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureEllipticCohomologySO _ellipticCohomologySO;
        [SerializeField] private PlayerWallet                             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onEllipticCohomologyComputed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _modularFormLabel;
        [SerializeField] private Text       _computeLabel;
        [SerializeField] private Slider     _modularFormBar;
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
            _onEllipticCohomologyComputed?.RegisterCallback(_handleComputedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onEllipticCohomologyComputed?.UnregisterCallback(_handleComputedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_ellipticCohomologySO == null) return;
            int bonus = _ellipticCohomologySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_ellipticCohomologySO == null) return;
            _ellipticCohomologySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _ellipticCohomologySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_ellipticCohomologySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_modularFormLabel != null)
                _modularFormLabel.text = $"Modular Forms: {_ellipticCohomologySO.ModularForms}/{_ellipticCohomologySO.ModularFormsNeeded}";

            if (_computeLabel != null)
                _computeLabel.text = $"Computations: {_ellipticCohomologySO.ComputationCount}";

            if (_modularFormBar != null)
                _modularFormBar.value = _ellipticCohomologySO.ModularFormProgress;
        }

        public ZoneControlCaptureEllipticCohomologySO EllipticCohomologySO => _ellipticCohomologySO;
    }
}

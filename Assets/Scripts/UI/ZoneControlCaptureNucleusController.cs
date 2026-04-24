using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureNucleusController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureNucleusSO _nucleusSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNucleusClosed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _morphismLabel;
        [SerializeField] private Text       _closureCountLabel;
        [SerializeField] private Slider     _morphismBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleNucleusClosedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleNucleusClosedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNucleusClosed?.RegisterCallback(_handleNucleusClosedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNucleusClosed?.UnregisterCallback(_handleNucleusClosedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_nucleusSO == null) return;
            int bonus = _nucleusSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_nucleusSO == null) return;
            _nucleusSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _nucleusSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_nucleusSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_morphismLabel != null)
                _morphismLabel.text = $"Morphisms: {_nucleusSO.Morphisms}/{_nucleusSO.MorphismsNeeded}";

            if (_closureCountLabel != null)
                _closureCountLabel.text = $"Closures: {_nucleusSO.ClosureCount}";

            if (_morphismBar != null)
                _morphismBar.value = _nucleusSO.MorphismProgress;
        }

        public ZoneControlCaptureNucleusSO NucleusSO => _nucleusSO;
    }
}

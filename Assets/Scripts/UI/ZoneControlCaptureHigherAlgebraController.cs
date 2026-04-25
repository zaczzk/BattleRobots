using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureHigherAlgebraController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureHigherAlgebraSO _higherAlgebraSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHigherAlgebraComposed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _structureMapLabel;
        [SerializeField] private Text       _composeLabel;
        [SerializeField] private Slider     _structureMapBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleComposedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleComposedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHigherAlgebraComposed?.RegisterCallback(_handleComposedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHigherAlgebraComposed?.UnregisterCallback(_handleComposedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_higherAlgebraSO == null) return;
            int bonus = _higherAlgebraSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_higherAlgebraSO == null) return;
            _higherAlgebraSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _higherAlgebraSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_higherAlgebraSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_structureMapLabel != null)
                _structureMapLabel.text = $"Structure Maps: {_higherAlgebraSO.StructureMaps}/{_higherAlgebraSO.StructureMapsNeeded}";

            if (_composeLabel != null)
                _composeLabel.text = $"Compositions: {_higherAlgebraSO.CompositionCount}";

            if (_structureMapBar != null)
                _structureMapBar.value = _higherAlgebraSO.StructureMapProgress;
        }

        public ZoneControlCaptureHigherAlgebraSO HigherAlgebraSO => _higherAlgebraSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFibreController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFibreSO _fibreSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFibreProjected;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _fibreLabel;
        [SerializeField] private Text       _projectionLabel;
        [SerializeField] private Slider     _fibreBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleProjectedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleProjectedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFibreProjected?.RegisterCallback(_handleProjectedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFibreProjected?.UnregisterCallback(_handleProjectedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_fibreSO == null) return;
            int bonus = _fibreSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_fibreSO == null) return;
            _fibreSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _fibreSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_fibreSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_fibreLabel != null)
                _fibreLabel.text = $"Fibres: {_fibreSO.Fibres}/{_fibreSO.FibresNeeded}";

            if (_projectionLabel != null)
                _projectionLabel.text = $"Projections: {_fibreSO.ProjectionCount}";

            if (_fibreBar != null)
                _fibreBar.value = _fibreSO.FibreProgress;
        }

        public ZoneControlCaptureFibreSO FibreSO => _fibreSO;
    }
}

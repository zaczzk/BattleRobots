using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCollatzConjectureController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCollatzConjectureSO _collatzSO;
        [SerializeField] private PlayerWallet                          _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCollatzConjectureConverged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _convergenceStepLabel;
        [SerializeField] private Text       _convergenceLabel;
        [SerializeField] private Slider     _convergenceStepBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleConvergenceDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleConvergenceDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCollatzConjectureConverged?.RegisterCallback(_handleConvergenceDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCollatzConjectureConverged?.UnregisterCallback(_handleConvergenceDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_collatzSO == null) return;
            int bonus = _collatzSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_collatzSO == null) return;
            _collatzSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _collatzSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_collatzSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_convergenceStepLabel != null)
                _convergenceStepLabel.text = $"Convergence Steps: {_collatzSO.ConvergenceSteps}/{_collatzSO.ConvergenceStepsNeeded}";

            if (_convergenceLabel != null)
                _convergenceLabel.text = $"Convergences: {_collatzSO.ConvergenceCount}";

            if (_convergenceStepBar != null)
                _convergenceStepBar.value = _collatzSO.ConvergenceStepProgress;
        }

        public ZoneControlCaptureCollatzConjectureSO CollatzSO => _collatzSO;
    }
}

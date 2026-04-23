using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFibrationController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFibrationSO _fibrationSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFibrationLifted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _fiberLabel;
        [SerializeField] private Text       _liftLabel;
        [SerializeField] private Slider     _fiberBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleLiftedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleLiftedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFibrationLifted?.RegisterCallback(_handleLiftedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFibrationLifted?.UnregisterCallback(_handleLiftedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_fibrationSO == null) return;
            int bonus = _fibrationSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_fibrationSO == null) return;
            _fibrationSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _fibrationSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_fibrationSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_fiberLabel != null)
                _fiberLabel.text = $"Fibers: {_fibrationSO.Fibers}/{_fibrationSO.FibersNeeded}";

            if (_liftLabel != null)
                _liftLabel.text = $"Lifts: {_fibrationSO.LiftCount}";

            if (_fiberBar != null)
                _fiberBar.value = _fibrationSO.FiberProgress;
        }

        public ZoneControlCaptureFibrationSO FibrationSO => _fibrationSO;
    }
}

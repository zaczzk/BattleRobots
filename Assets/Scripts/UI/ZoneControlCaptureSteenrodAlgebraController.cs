using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSteenrodAlgebraController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSteenrodAlgebraSO _steenrodAlgebraSO;
        [SerializeField] private PlayerWallet                         _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSteenrodAlgebraApplied;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _sqOpLabel;
        [SerializeField] private Text       _applyLabel;
        [SerializeField] private Slider     _sqOpBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleAppliedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleAppliedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSteenrodAlgebraApplied?.RegisterCallback(_handleAppliedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSteenrodAlgebraApplied?.UnregisterCallback(_handleAppliedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_steenrodAlgebraSO == null) return;
            int bonus = _steenrodAlgebraSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_steenrodAlgebraSO == null) return;
            _steenrodAlgebraSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _steenrodAlgebraSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_steenrodAlgebraSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_sqOpLabel != null)
                _sqOpLabel.text = $"Sq Ops: {_steenrodAlgebraSO.SqOps}/{_steenrodAlgebraSO.SqOpsNeeded}";

            if (_applyLabel != null)
                _applyLabel.text = $"Applications: {_steenrodAlgebraSO.ApplicationCount}";

            if (_sqOpBar != null)
                _sqOpBar.value = _steenrodAlgebraSO.SqOpProgress;
        }

        public ZoneControlCaptureSteenrodAlgebraSO SteenrodAlgebraSO => _steenrodAlgebraSO;
    }
}

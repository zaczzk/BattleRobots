using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCartesianController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCartesianSO _cartesianSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDiagonalized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _projectionLabel;
        [SerializeField] private Text       _diagonalLabel;
        [SerializeField] private Slider     _projectionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDiagonalizedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDiagonalizedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDiagonalized?.RegisterCallback(_handleDiagonalizedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDiagonalized?.UnregisterCallback(_handleDiagonalizedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cartesianSO == null) return;
            int bonus = _cartesianSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cartesianSO == null) return;
            _cartesianSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cartesianSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cartesianSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_projectionLabel != null)
                _projectionLabel.text = $"Projections: {_cartesianSO.Projections}/{_cartesianSO.ProjectionsNeeded}";

            if (_diagonalLabel != null)
                _diagonalLabel.text = $"Diagonalizations: {_cartesianSO.DiagonalizeCount}";

            if (_projectionBar != null)
                _projectionBar.value = _cartesianSO.ProjectionProgress;
        }

        public ZoneControlCaptureCartesianSO CartesianSO => _cartesianSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureStableInftyCatController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureStableInftyCatSO _stableInftyCatSO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onStableInftyCatStabilized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _triangleLabel;
        [SerializeField] private Text       _stabilizeLabel;
        [SerializeField] private Slider     _triangleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleStabilizedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleStabilizedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onStableInftyCatStabilized?.RegisterCallback(_handleStabilizedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onStableInftyCatStabilized?.UnregisterCallback(_handleStabilizedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_stableInftyCatSO == null) return;
            int bonus = _stableInftyCatSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_stableInftyCatSO == null) return;
            _stableInftyCatSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _stableInftyCatSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_stableInftyCatSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_triangleLabel != null)
                _triangleLabel.text = $"Triangles: {_stableInftyCatSO.ExactTriangles}/{_stableInftyCatSO.ExactTrianglesNeeded}";

            if (_stabilizeLabel != null)
                _stabilizeLabel.text = $"Stabilizations: {_stableInftyCatSO.StabilizeCount}";

            if (_triangleBar != null)
                _triangleBar.value = _stableInftyCatSO.ExactTriangleProgress;
        }

        public ZoneControlCaptureStableInftyCatSO StableInftyCatSO => _stableInftyCatSO;
    }
}

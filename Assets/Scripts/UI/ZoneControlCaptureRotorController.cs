using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRotorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRotorSO _rotorSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRotorRevolved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _segmentLabel;
        [SerializeField] private Text       _revolutionLabel;
        [SerializeField] private Slider     _segmentBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRevolvedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRevolvedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRotorRevolved?.RegisterCallback(_handleRevolvedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRotorRevolved?.UnregisterCallback(_handleRevolvedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_rotorSO == null) return;
            int bonus = _rotorSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_rotorSO == null) return;
            _rotorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _rotorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_rotorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_segmentLabel != null)
                _segmentLabel.text = $"Segments: {_rotorSO.Segments}/{_rotorSO.SegmentsNeeded}";

            if (_revolutionLabel != null)
                _revolutionLabel.text = $"Revolutions: {_rotorSO.RevolutionCount}";

            if (_segmentBar != null)
                _segmentBar.value = _rotorSO.SegmentProgress;
        }

        public ZoneControlCaptureRotorSO RotorSO => _rotorSO;
    }
}

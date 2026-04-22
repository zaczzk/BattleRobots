using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFrameController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFrameSO _frameSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFrameTransmitted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _frameLabel;
        [SerializeField] private Text       _transmitLabel;
        [SerializeField] private Slider     _frameBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTransmittedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleTransmittedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFrameTransmitted?.RegisterCallback(_handleTransmittedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFrameTransmitted?.UnregisterCallback(_handleTransmittedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_frameSO == null) return;
            int bonus = _frameSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_frameSO == null) return;
            _frameSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _frameSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_frameSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_frameLabel != null)
                _frameLabel.text = $"Frames: {_frameSO.Frames}/{_frameSO.FramesNeeded}";

            if (_transmitLabel != null)
                _transmitLabel.text = $"Transmits: {_frameSO.TransmitCount}";

            if (_frameBar != null)
                _frameBar.value = _frameSO.FrameProgress;
        }

        public ZoneControlCaptureFrameSO FrameSO => _frameSO;
    }
}

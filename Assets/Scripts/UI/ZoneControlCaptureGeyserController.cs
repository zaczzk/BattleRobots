using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGeyserController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGeyserSO _geyserSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onEruption;
        [SerializeField] private VoidGameEvent _onGeyserComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _eruptionLabel;
        [SerializeField] private Slider     _buildBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleEruptionDelegate;
        private Action _handleCompleteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleEruptionDelegate     = Refresh;
            _handleCompleteDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onEruption?.RegisterCallback(_handleEruptionDelegate);
            _onGeyserComplete?.RegisterCallback(_handleCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onEruption?.UnregisterCallback(_handleEruptionDelegate);
            _onGeyserComplete?.UnregisterCallback(_handleCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_geyserSO == null) return;
            int bonus = _geyserSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_geyserSO == null) return;
            _geyserSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _geyserSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_geyserSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _geyserSO.IsComplete
                    ? "COMPLETE!"
                    : $"Pressure: {_geyserSO.BuildCount}/{_geyserSO.CapturesForEruption}";

            if (_eruptionLabel != null)
                _eruptionLabel.text = $"Eruptions: {_geyserSO.EruptionCount}/{_geyserSO.MaxEruptions}";

            if (_buildBar != null)
                _buildBar.value = _geyserSO.BuildProgress;
        }

        public ZoneControlCaptureGeyserSO GeyserSO => _geyserSO;
    }
}

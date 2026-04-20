using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureVortexController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureVortexSO _vortexSO;
        [SerializeField] private PlayerWalletSO              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onVortexOpened;
        [SerializeField] private VoidGameEvent _onVortexClosed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _vortexCountLabel;
        [SerializeField] private Slider     _vortexBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleVortexOpenedDelegate;
        private Action _handleVortexClosedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleVortexOpenedDelegate = Refresh;
            _handleVortexClosedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onVortexOpened?.RegisterCallback(_handleVortexOpenedDelegate);
            _onVortexClosed?.RegisterCallback(_handleVortexClosedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onVortexOpened?.UnregisterCallback(_handleVortexOpenedDelegate);
            _onVortexClosed?.UnregisterCallback(_handleVortexClosedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_vortexSO == null) return;
            int bonus = _vortexSO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_vortexSO == null) return;
            _vortexSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _vortexSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_vortexSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
            {
                _statusLabel.text = _vortexSO.IsActive
                    ? $"VORTEX ACTIVE! {_vortexSO.CapturesRemaining} left"
                    : $"Charging: {_vortexSO.BotChargeCount}/{_vortexSO.ChargesForVortex}";
            }

            if (_vortexCountLabel != null)
                _vortexCountLabel.text = $"Vortexes: {_vortexSO.VortexCount}";

            if (_vortexBar != null)
                _vortexBar.value = _vortexSO.VortexProgress;
        }

        public ZoneControlCaptureVortexSO VortexSO => _vortexSO;
    }
}

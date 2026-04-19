using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRicochetController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRicochetSO _ricochetSO;
        [SerializeField] private PlayerWalletSO               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRicochet;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _countLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleBotDelegate;
        private Action _handlePlayerDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRicochetDelegate;

        private void Awake()
        {
            _handleBotDelegate          = HandleBotCaptured;
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRicochetDelegate     = HandleRicochet;
        }

        private void OnEnable()
        {
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRicochet?.RegisterCallback(_handleRicochetDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRicochet?.UnregisterCallback(_handleRicochetDelegate);
        }

        private void HandleBotCaptured()
        {
            if (_ricochetSO == null) return;
            _ricochetSO.RecordBotCapture();
            Refresh();
        }

        private void HandlePlayerCaptured()
        {
            if (_ricochetSO == null) return;
            int prev = _ricochetSO.RicochetCount;
            _ricochetSO.RecordPlayerCapture();
            if (_ricochetSO.RicochetCount > prev)
                _wallet?.AddFunds(_ricochetSO.RicochetBonus);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _ricochetSO?.Reset();
            Refresh();
        }

        private void HandleRicochet()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_ricochetSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _ricochetSO.IsArmed ? "Ricochet Ready!" : "No Ricochet";

            if (_countLabel != null)
                _countLabel.text = $"Ricochets: {_ricochetSO.RicochetCount}";
        }

        public ZoneControlCaptureRicochetSO RicochetSO => _ricochetSO;
    }
}

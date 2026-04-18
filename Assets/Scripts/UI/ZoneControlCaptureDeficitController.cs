using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDeficitController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDeficitSO _deficitSO;

        [Header("Economy (optional)")]
        [SerializeField] private PlayerWallet _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHighDeficit;
        [SerializeField] private VoidGameEvent _onDeficitCleared;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _deficitLabel;
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleHighDeficitDelegate;
        private Action _handleDeficitClearedDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate  = HandlePlayerCaptured;
            _handleBotCapturedDelegate     = HandleBotCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handleHighDeficitDelegate     = Refresh;
            _handleDeficitClearedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHighDeficit?.RegisterCallback(_handleHighDeficitDelegate);
            _onDeficitCleared?.RegisterCallback(_handleDeficitClearedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHighDeficit?.UnregisterCallback(_handleHighDeficitDelegate);
            _onDeficitCleared?.UnregisterCallback(_handleDeficitClearedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_deficitSO == null) return;
            _deficitSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_deficitSO == null) return;
            _deficitSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _deficitSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_deficitSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_deficitLabel != null)
                _deficitLabel.text = $"Deficit: {_deficitSO.Deficit:+0;-0;0}";

            if (_statusLabel != null)
                _statusLabel.text = _deficitSO.IsHighDeficit ? "HIGH" : "OK";
        }

        public ZoneControlCaptureDeficitSO DeficitSO => _deficitSO;
    }
}

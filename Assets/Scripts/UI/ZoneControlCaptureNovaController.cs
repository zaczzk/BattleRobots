using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureNovaController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureNovaSO _novaSO;
        [SerializeField] private PlayerWalletSO           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNova;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _poolLabel;
        [SerializeField] private Text       _novaCountLabel;
        [SerializeField] private Slider     _novaBar;
        [SerializeField] private GameObject _panel;

        private Action _handleCaptureDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleNovaDelegate;

        private void Awake()
        {
            _handleCaptureDelegate      = HandleCapture;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleNovaDelegate         = HandleNova;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleCaptureDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNova?.RegisterCallback(_handleNovaDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleCaptureDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNova?.UnregisterCallback(_handleNovaDelegate);
        }

        private void Update()
        {
            if (_novaSO == null) return;
            _novaSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleCapture()
        {
            if (_novaSO == null) return;
            int prev = _novaSO.NovaCount;
            _novaSO.RecordCapture();
            if (_novaSO.NovaCount > prev)
                _wallet?.AddFunds(_novaSO.BonusPerNova);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _novaSO?.Reset();
            Refresh();
        }

        private void HandleNova() => Refresh();

        public void Refresh()
        {
            if (_novaSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_poolLabel != null)
                _poolLabel.text = $"Nova Pool: {_novaSO.NovaPool:F1}/{_novaSO.NovaThreshold:F0}";

            if (_novaCountLabel != null)
                _novaCountLabel.text = $"Novas: {_novaSO.NovaCount}";

            if (_novaBar != null)
                _novaBar.value = _novaSO.NovaProgress;
        }

        public ZoneControlCaptureNovaSO NovaSO => _novaSO;
    }
}

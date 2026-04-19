using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureAuraController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureAuraSO _auraSO;
        [SerializeField] private PlayerWalletSO           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onAuraActivated;
        [SerializeField] private VoidGameEvent _onAuraDepleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _auraLabel;
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Slider     _auraBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleMatchEndedDelegate     = HandleMatchEnded;
            _refreshDelegate              = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onAuraActivated?.RegisterCallback(_refreshDelegate);
            _onAuraDepleted?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onAuraActivated?.UnregisterCallback(_refreshDelegate);
            _onAuraDepleted?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            _auraSO?.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandlePlayerCaptured()
        {
            if (_auraSO == null) return;
            int bonus = _auraSO.RecordCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _auraSO?.Reset();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            _auraSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_auraSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_auraLabel != null)
                _auraLabel.text = $"Aura: {_auraSO.AuraProgress * 100f:F1}%";

            if (_statusLabel != null)
                _statusLabel.text = _auraSO.IsAuraActive ? "Aura Active!" : "Standby";

            if (_auraBar != null)
                _auraBar.value = _auraSO.AuraProgress;
        }

        public ZoneControlCaptureAuraSO AuraSO => _auraSO;
    }
}

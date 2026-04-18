using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCooldownController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCooldownSO _cooldownSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCooldownStarted;
        [SerializeField] private VoidGameEvent _onCooldownExpired;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Slider     _cooldownBar;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCooldownStarted?.RegisterCallback(_refreshDelegate);
            _onCooldownExpired?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCooldownStarted?.UnregisterCallback(_refreshDelegate);
            _onCooldownExpired?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (_cooldownSO == null || !_cooldownSO.IsOnCooldown) return;
            _cooldownSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            _cooldownSO?.StartCooldown();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cooldownSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cooldownSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _cooldownSO.IsOnCooldown
                    ? $"Cooldown: {_cooldownSO.RemainingTime:F1}s"
                    : "Ready";

            if (_cooldownBar != null)
                _cooldownBar.value = _cooldownSO.CooldownProgress;
        }

        public ZoneControlCaptureCooldownSO CooldownSO => _cooldownSO;
    }
}

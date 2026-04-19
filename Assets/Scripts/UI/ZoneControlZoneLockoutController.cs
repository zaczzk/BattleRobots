using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneLockoutController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneLockoutSO _lockoutSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLockoutStarted;
        [SerializeField] private VoidGameEvent _onLockoutExpired;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Slider     _lockoutBar;
        [SerializeField] private GameObject _panel;

        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleLockoutDelegate;

        private void Awake()
        {
            _handleBotCapturedDelegate  = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleLockoutDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLockoutStarted?.RegisterCallback(_handleLockoutDelegate);
            _onLockoutExpired?.RegisterCallback(_handleLockoutDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLockoutStarted?.UnregisterCallback(_handleLockoutDelegate);
            _onLockoutExpired?.UnregisterCallback(_handleLockoutDelegate);
        }

        private void Update()
        {
            if (_lockoutSO == null || !_lockoutSO.IsLockedOut) return;
            _lockoutSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_lockoutSO == null) return;
            _lockoutSO.StartLockout();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _lockoutSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_lockoutSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _lockoutSO.IsLockedOut
                    ? $"Locked: {_lockoutSO.RemainingTime:F1}s"
                    : "Open";

            if (_lockoutBar != null)
                _lockoutBar.value = _lockoutSO.LockoutProgress;
        }

        public ZoneControlZoneLockoutSO LockoutSO => _lockoutSO;
    }
}

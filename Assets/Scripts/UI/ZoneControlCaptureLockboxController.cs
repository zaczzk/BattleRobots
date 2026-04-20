using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLockboxController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLockboxSO _lockboxSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLockboxOpened;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _lockLabel;
        [SerializeField] private Text       _openLabel;
        [SerializeField] private Slider     _lockBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleLockboxOpenedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleLockboxOpenedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLockboxOpened?.RegisterCallback(_handleLockboxOpenedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLockboxOpened?.UnregisterCallback(_handleLockboxOpenedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_lockboxSO == null) return;
            int bonus = _lockboxSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_lockboxSO == null) return;
            _lockboxSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _lockboxSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_lockboxSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_lockLabel != null)
                _lockLabel.text = $"Locks: {_lockboxSO.CurrentLocks}";

            if (_openLabel != null)
                _openLabel.text = $"Openings: {_lockboxSO.OpenCount}";

            if (_lockBar != null)
                _lockBar.value = _lockboxSO.LockProgress;
        }

        public ZoneControlCaptureLockboxSO LockboxSO => _lockboxSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSurgeWindowController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSurgeWindowSO _surgeSO;
        [SerializeField] private PlayerWalletSO                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSurgeOpened;
        [SerializeField] private VoidGameEvent _onSurgeClosed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _surgeCountLabel;
        [SerializeField] private Slider     _surgeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSurgeOpenedDelegate;
        private Action _handleSurgeClosedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSurgeOpenedDelegate  = Refresh;
            _handleSurgeClosedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSurgeOpened?.RegisterCallback(_handleSurgeOpenedDelegate);
            _onSurgeClosed?.RegisterCallback(_handleSurgeClosedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSurgeOpened?.UnregisterCallback(_handleSurgeOpenedDelegate);
            _onSurgeClosed?.UnregisterCallback(_handleSurgeClosedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_surgeSO == null) return;
            int bonus = _surgeSO.RecordPlayerCapture();
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_surgeSO == null) return;
            _surgeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _surgeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_surgeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _surgeSO.IsSurgeActive
                    ? $"SURGE ACTIVE! {_surgeSO.SurgePlayerCaptures - _surgeSO.PlayerCapturesDuringSurge} left"
                    : $"Building: {_surgeSO.BotStreak}/{_surgeSO.BotTriggerCount}";

            if (_surgeCountLabel != null)
                _surgeCountLabel.text = $"Surges: {_surgeSO.SurgeCount}";

            if (_surgeBar != null)
                _surgeBar.value = _surgeSO.SurgeProgress;
        }

        public ZoneControlCaptureSurgeWindowSO SurgeSO => _surgeSO;
    }
}

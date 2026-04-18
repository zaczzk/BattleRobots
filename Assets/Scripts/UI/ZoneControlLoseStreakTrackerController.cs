using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlLoseStreakTrackerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlLoseStreakTrackerSO _trackerSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLoseStreakWarning;
        [SerializeField] private VoidGameEvent _onLoseStreakReset;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _streakLabel;
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleBotCaptureDelegate;
        private Action _handlePlayerCaptureDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleBotCaptureDelegate    = HandleBotCapture;
            _handlePlayerCaptureDelegate = HandlePlayerCapture;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _refreshDelegate             = Refresh;
        }

        private void OnEnable()
        {
            _onBotZoneCaptured?.RegisterCallback(_handleBotCaptureDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCaptureDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLoseStreakWarning?.RegisterCallback(_refreshDelegate);
            _onLoseStreakReset?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCaptureDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCaptureDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLoseStreakWarning?.UnregisterCallback(_refreshDelegate);
            _onLoseStreakReset?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleBotCapture()
        {
            _trackerSO?.RecordBotCapture();
            Refresh();
        }

        private void HandlePlayerCapture()
        {
            _trackerSO?.RecordPlayerCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _trackerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_trackerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_streakLabel != null)
                _streakLabel.text = $"Lost Streak: {_trackerSO.LoseStreak}";

            if (_statusLabel != null)
                _statusLabel.text = _trackerSO.IsWarning ? "WARNING!" : "Normal";
        }

        public ZoneControlLoseStreakTrackerSO TrackerSO => _trackerSO;
    }
}

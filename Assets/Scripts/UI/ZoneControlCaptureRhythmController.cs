using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRhythmController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRhythmSO _rhythmSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRhythmAchieved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _rhythmStreakLabel;
        [SerializeField] private Text       _rhythmCountLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRhythmAchievedDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate   = HandleZoneCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleRhythmAchievedDelegate = HandleRhythmAchieved;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRhythmAchieved?.RegisterCallback(_handleRhythmAchievedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRhythmAchieved?.UnregisterCallback(_handleRhythmAchievedDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_rhythmSO == null) return;
            _rhythmSO.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _rhythmSO?.Reset();
            Refresh();
        }

        private void HandleRhythmAchieved()
        {
            if (_rhythmSO == null) return;
            _wallet?.AddFunds(_rhythmSO.BonusPerRhythmStreak);
            Refresh();
        }

        public void Refresh()
        {
            if (_rhythmSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_rhythmStreakLabel != null)
                _rhythmStreakLabel.text = $"Rhythm Streak: {_rhythmSO.RhythmStreak}";

            if (_rhythmCountLabel != null)
                _rhythmCountLabel.text = $"Rhythms: {_rhythmSO.RhythmCount}";
        }

        public ZoneControlCaptureRhythmSO RhythmSO => _rhythmSO;
    }
}

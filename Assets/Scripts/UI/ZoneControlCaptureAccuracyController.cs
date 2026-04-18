using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureAccuracyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureAccuracySO _accuracySO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPlayerZoneAttempted;
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onAccuracyChanged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _accuracyLabel;
        [SerializeField] private Slider     _accuracyBar;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchStartedDelegate;
        private Action _handleAttemptDelegate;
        private Action _handleSuccessDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleAttemptDelegate      = HandleAttempt;
            _handleSuccessDelegate      = HandleSuccess;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPlayerZoneAttempted?.RegisterCallback(_handleAttemptDelegate);
            _onPlayerZoneCaptured?.RegisterCallback(_handleSuccessDelegate);
            _onAccuracyChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPlayerZoneAttempted?.UnregisterCallback(_handleAttemptDelegate);
            _onPlayerZoneCaptured?.UnregisterCallback(_handleSuccessDelegate);
            _onAccuracyChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleMatchStarted()
        {
            _accuracySO?.Reset();
            Refresh();
        }

        private void HandleAttempt()
        {
            _accuracySO?.RecordAttempt();
            Refresh();
        }

        private void HandleSuccess()
        {
            _accuracySO?.RecordSuccess();
            Refresh();
        }

        public void Refresh()
        {
            if (_accuracySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_accuracyLabel != null)
                _accuracyLabel.text = $"Accuracy: {Mathf.RoundToInt(_accuracySO.Accuracy * 100)}%";

            if (_accuracyBar != null)
                _accuracyBar.value = _accuracySO.Accuracy;
        }

        public ZoneControlCaptureAccuracySO AccuracySO => _accuracySO;
    }
}

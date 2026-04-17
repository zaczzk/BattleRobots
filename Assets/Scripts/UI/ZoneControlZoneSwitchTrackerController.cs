using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that wires zone-capture events into
    /// <see cref="ZoneControlZoneSwitchTrackerSO"/> and shows a "Switches: N"
    /// label and a FREQUENT SWITCHER / Building... status badge.
    ///
    /// <c>_onPlayerZoneCaptured</c> (IntGameEvent): RecordCapture(index) + Refresh.
    /// <c>_onMatchStarted</c>: Reset + Refresh.
    /// <c>_onSwitchRecorded</c> / <c>_onFrequentSwitcher</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneSwitchTrackerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneSwitchTrackerSO _switchTrackerSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private IntGameEvent  _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSwitchRecorded;
        [SerializeField] private VoidGameEvent _onFrequentSwitcher;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _switchesLabel;
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private GameObject _panel;

        private Action<int> _handleCaptureDelegate;
        private Action      _handleMatchStartedDelegate;
        private Action      _refreshDelegate;

        private void Awake()
        {
            _handleCaptureDelegate     = HandlePlayerZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handleCaptureDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSwitchRecorded?.RegisterCallback(_refreshDelegate);
            _onFrequentSwitcher?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handleCaptureDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSwitchRecorded?.UnregisterCallback(_refreshDelegate);
            _onFrequentSwitcher?.UnregisterCallback(_refreshDelegate);
        }

        private void HandlePlayerZoneCaptured(int zoneIndex)
        {
            _switchTrackerSO?.RecordCapture(zoneIndex);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _switchTrackerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_switchTrackerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_switchesLabel != null)
                _switchesLabel.text = $"Switches: {_switchTrackerSO.SwitchCount}";

            if (_statusLabel != null)
                _statusLabel.text = _switchTrackerSO.IsFrequentSwitcher
                    ? "FREQUENT SWITCHER"
                    : "Building...";
        }

        public ZoneControlZoneSwitchTrackerSO SwitchTrackerSO => _switchTrackerSO;
    }
}

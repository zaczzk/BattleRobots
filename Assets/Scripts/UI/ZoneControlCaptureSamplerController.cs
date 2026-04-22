using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSamplerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSamplerSO _samplerSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSamplerRecorded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _recordingLabel;
        [SerializeField] private Text       _recordLabel;
        [SerializeField] private Slider     _recordingBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSamplerDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSamplerDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSamplerRecorded?.RegisterCallback(_handleSamplerDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSamplerRecorded?.UnregisterCallback(_handleSamplerDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_samplerSO == null) return;
            int bonus = _samplerSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_samplerSO == null) return;
            _samplerSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _samplerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_samplerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_recordingLabel != null)
                _recordingLabel.text = $"Recordings: {_samplerSO.Recordings}/{_samplerSO.RecordingsNeeded}";

            if (_recordLabel != null)
                _recordLabel.text = $"Records: {_samplerSO.RecordCount}";

            if (_recordingBar != null)
                _recordingBar.value = _samplerSO.RecordingProgress;
        }

        public ZoneControlCaptureSamplerSO SamplerSO => _samplerSO;
    }
}

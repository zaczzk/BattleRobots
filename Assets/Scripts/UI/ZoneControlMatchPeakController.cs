using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchPeakController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchPeakSO _peakSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNewPeak;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _peakLabel;
        [SerializeField] private Text       _currentLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCapturedDelegate;
        private Action _handleBotCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleNewPeakDelegate;

        private void Awake()
        {
            _handlePlayerCapturedDelegate = HandlePlayerCaptured;
            _handleBotCapturedDelegate    = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleNewPeakDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNewPeak?.RegisterCallback(_handleNewPeakDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNewPeak?.UnregisterCallback(_handleNewPeakDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_peakSO == null) return;
            _peakSO.RecordPlayerCapture();
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_peakSO == null) return;
            _peakSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _peakSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_peakSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_peakLabel != null)
                _peakLabel.text = $"Peak: {_peakSO.PeakStreak}";

            if (_currentLabel != null)
                _currentLabel.text = $"Current: {_peakSO.CurrentStreak}";
        }

        public ZoneControlMatchPeakSO PeakSO => _peakSO;
    }
}

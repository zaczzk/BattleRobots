using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlRivalTrackerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlRivalTrackerSO _rivalTrackerSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onRivalZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRivalLeading;
        [SerializeField] private VoidGameEvent _onPlayerLeading;
        [SerializeField] private VoidGameEvent _onCapturesUpdated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _scoreLabel;
        [SerializeField] private Text       _leadLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerCaptureDelegate;
        private Action _handleRivalCaptureDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handlePlayerCaptureDelegate = HandlePlayerCapture;
            _handleRivalCaptureDelegate  = HandleRivalCapture;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _refreshDelegate             = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerCaptureDelegate);
            _onRivalZoneCaptured?.RegisterCallback(_handleRivalCaptureDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRivalLeading?.RegisterCallback(_refreshDelegate);
            _onPlayerLeading?.RegisterCallback(_refreshDelegate);
            _onCapturesUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerCaptureDelegate);
            _onRivalZoneCaptured?.UnregisterCallback(_handleRivalCaptureDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRivalLeading?.UnregisterCallback(_refreshDelegate);
            _onPlayerLeading?.UnregisterCallback(_refreshDelegate);
            _onCapturesUpdated?.UnregisterCallback(_refreshDelegate);
        }

        private void HandlePlayerCapture()
        {
            _rivalTrackerSO?.RecordPlayerCapture();
            Refresh();
        }

        private void HandleRivalCapture()
        {
            _rivalTrackerSO?.RecordRivalCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _rivalTrackerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_rivalTrackerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_scoreLabel != null)
                _scoreLabel.text = $"Player: {_rivalTrackerSO.PlayerCaptures} / Rival: {_rivalTrackerSO.RivalCaptures}";

            if (_leadLabel != null)
                _leadLabel.text = _rivalTrackerSO.IsRivalLeading ? "RIVAL LEADS!" : "Player Leads";
        }

        public ZoneControlRivalTrackerSO RivalTrackerSO => _rivalTrackerSO;
    }
}

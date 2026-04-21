using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCompassController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCompassSO _compassSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCompassNavigated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bearingLabel;
        [SerializeField] private Text       _navigationLabel;
        [SerializeField] private Slider     _bearingBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompassNavigatedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate             = HandlePlayerCaptured;
            _handleBotDelegate                = HandleBotCaptured;
            _handleMatchStartedDelegate       = HandleMatchStarted;
            _handleCompassNavigatedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCompassNavigated?.RegisterCallback(_handleCompassNavigatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCompassNavigated?.UnregisterCallback(_handleCompassNavigatedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_compassSO == null) return;
            int bonus = _compassSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_compassSO == null) return;
            _compassSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _compassSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_compassSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bearingLabel != null)
                _bearingLabel.text = $"Bearings: {_compassSO.Bearings}/{_compassSO.BearingsNeeded}";

            if (_navigationLabel != null)
                _navigationLabel.text = $"Navigations: {_compassSO.NavigationCount}";

            if (_bearingBar != null)
                _bearingBar.value = _compassSO.BearingProgress;
        }

        public ZoneControlCaptureCompassSO CompassSO => _compassSO;
    }
}

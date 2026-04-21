using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureHourglassController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureHourglassSO _hourglassSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHourglassInverted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _grainLabel;
        [SerializeField] private Text       _inversionLabel;
        [SerializeField] private Slider     _grainBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleHourglassInvertedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate           = HandlePlayerCaptured;
            _handleBotDelegate              = HandleBotCaptured;
            _handleMatchStartedDelegate     = HandleMatchStarted;
            _handleHourglassInvertedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHourglassInverted?.RegisterCallback(_handleHourglassInvertedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHourglassInverted?.UnregisterCallback(_handleHourglassInvertedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_hourglassSO == null) return;
            int bonus = _hourglassSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_hourglassSO == null) return;
            _hourglassSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _hourglassSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_hourglassSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_grainLabel != null)
                _grainLabel.text = $"Grains: {_hourglassSO.Grains}/{_hourglassSO.GrainsNeeded}";

            if (_inversionLabel != null)
                _inversionLabel.text = $"Inversions: {_hourglassSO.InversionCount}";

            if (_grainBar != null)
                _grainBar.value = _hourglassSO.GrainProgress;
        }

        public ZoneControlCaptureHourglassSO HourglassSO => _hourglassSO;
    }
}

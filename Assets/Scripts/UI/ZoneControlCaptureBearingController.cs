using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBearingController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBearingSO _bearingSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBearingSpun;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _raceLabel;
        [SerializeField] private Text       _spinLabel;
        [SerializeField] private Slider     _raceBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSpunDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSpunDelegate         = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBearingSpun?.RegisterCallback(_handleSpunDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBearingSpun?.UnregisterCallback(_handleSpunDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_bearingSO == null) return;
            int bonus = _bearingSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_bearingSO == null) return;
            _bearingSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _bearingSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_bearingSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_raceLabel != null)
                _raceLabel.text = $"Races: {_bearingSO.Races}/{_bearingSO.RacesNeeded}";

            if (_spinLabel != null)
                _spinLabel.text = $"Spins: {_bearingSO.SpinCount}";

            if (_raceBar != null)
                _raceBar.value = _bearingSO.RaceProgress;
        }

        public ZoneControlCaptureBearingSO BearingSO => _bearingSO;
    }
}

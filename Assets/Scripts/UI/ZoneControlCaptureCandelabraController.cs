using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCandelabraController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCandelabraSO _candelabraSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCandelabraIlluminated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _flameLabel;
        [SerializeField] private Text       _illuminationLabel;
        [SerializeField] private Slider     _flameBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleIlluminatedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate      = HandlePlayerCaptured;
            _handleBotDelegate         = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleIlluminatedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCandelabraIlluminated?.RegisterCallback(_handleIlluminatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCandelabraIlluminated?.UnregisterCallback(_handleIlluminatedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_candelabraSO == null) return;
            int bonus = _candelabraSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_candelabraSO == null) return;
            _candelabraSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _candelabraSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_candelabraSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_flameLabel != null)
                _flameLabel.text = $"Flames: {_candelabraSO.Flames}/{_candelabraSO.FlamesNeeded}";

            if (_illuminationLabel != null)
                _illuminationLabel.text = $"Illuminations: {_candelabraSO.IlluminationCount}";

            if (_flameBar != null)
                _flameBar.value = _candelabraSO.FlameProgress;
        }

        public ZoneControlCaptureCandelabraSO CandelabraSO => _candelabraSO;
    }
}

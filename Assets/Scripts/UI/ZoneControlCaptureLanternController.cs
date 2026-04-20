using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLanternController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLanternSO _lanternSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onIlluminated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _lanternLabel;
        [SerializeField] private Text       _illuminationLabel;
        [SerializeField] private Slider     _lanternBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleIlluminatedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleIlluminatedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onIlluminated?.RegisterCallback(_handleIlluminatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onIlluminated?.UnregisterCallback(_handleIlluminatedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_lanternSO == null) return;
            int bonus = _lanternSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_lanternSO == null) return;
            _lanternSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _lanternSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_lanternSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_lanternLabel != null)
                _lanternLabel.text = $"Lanterns: {_lanternSO.LitLanterns}/{_lanternSO.LanternsNeeded}";

            if (_illuminationLabel != null)
                _illuminationLabel.text = $"Illuminations: {_lanternSO.IlluminationCount}";

            if (_lanternBar != null)
                _lanternBar.value = _lanternSO.LanternProgress;
        }

        public ZoneControlCaptureLanternSO LanternSO => _lanternSO;
    }
}

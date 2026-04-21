using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureWinchController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureWinchSO _winchSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onWinchHauled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _crankLabel;
        [SerializeField] private Text       _haulLabel;
        [SerializeField] private Slider     _crankBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleHauledDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleHauledDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onWinchHauled?.RegisterCallback(_handleHauledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onWinchHauled?.UnregisterCallback(_handleHauledDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_winchSO == null) return;
            int bonus = _winchSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_winchSO == null) return;
            _winchSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _winchSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_winchSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_crankLabel != null)
                _crankLabel.text = $"Cranks: {_winchSO.Cranks}/{_winchSO.CranksNeeded}";

            if (_haulLabel != null)
                _haulLabel.text = $"Hauls: {_winchSO.HaulCount}";

            if (_crankBar != null)
                _crankBar.value = _winchSO.CrankProgress;
        }

        public ZoneControlCaptureWinchSO WinchSO => _winchSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureValveController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureValveSO _valveSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onValveOpened;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _stemLabel;
        [SerializeField] private Text       _openLabel;
        [SerializeField] private Slider     _stemBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleOpenedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleOpenedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onValveOpened?.RegisterCallback(_handleOpenedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onValveOpened?.UnregisterCallback(_handleOpenedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_valveSO == null) return;
            int bonus = _valveSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_valveSO == null) return;
            _valveSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _valveSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_valveSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_stemLabel != null)
                _stemLabel.text = $"Stems: {_valveSO.Stems}/{_valveSO.StemsNeeded}";

            if (_openLabel != null)
                _openLabel.text = $"Opens: {_valveSO.OpenCount}";

            if (_stemBar != null)
                _stemBar.value = _valveSO.StemProgress;
        }

        public ZoneControlCaptureValveSO ValveSO => _valveSO;
    }
}

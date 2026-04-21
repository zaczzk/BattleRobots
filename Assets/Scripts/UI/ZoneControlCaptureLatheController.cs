using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLatheController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLatheSO _latheSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLatheSpun;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _turningLabel;
        [SerializeField] private Text       _spinLabel;
        [SerializeField] private Slider     _turningBar;
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
            _onLatheSpun?.RegisterCallback(_handleSpunDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLatheSpun?.UnregisterCallback(_handleSpunDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_latheSO == null) return;
            int bonus = _latheSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_latheSO == null) return;
            _latheSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _latheSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_latheSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_turningLabel != null)
                _turningLabel.text = $"Turnings: {_latheSO.Turnings}/{_latheSO.TurningsNeeded}";

            if (_spinLabel != null)
                _spinLabel.text = $"Spins: {_latheSO.SpinCount}";

            if (_turningBar != null)
                _turningBar.value = _latheSO.TurningProgress;
        }

        public ZoneControlCaptureLatheSO LatheSO => _latheSO;
    }
}

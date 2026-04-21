using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLocketController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLocketSO _locketSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLocketFilled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _charmLabel;
        [SerializeField] private Text       _locketLabel;
        [SerializeField] private Slider     _charmBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleLocketFilledDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleLocketFilledDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLocketFilled?.RegisterCallback(_handleLocketFilledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLocketFilled?.UnregisterCallback(_handleLocketFilledDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_locketSO == null) return;
            int bonus = _locketSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_locketSO == null) return;
            _locketSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _locketSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_locketSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_charmLabel != null)
                _charmLabel.text = $"Charms: {_locketSO.Charms}/{_locketSO.CharmsNeeded}";

            if (_locketLabel != null)
                _locketLabel.text = $"Lockets: {_locketSO.LocketCount}";

            if (_charmBar != null)
                _charmBar.value = _locketSO.CharmProgress;
        }

        public ZoneControlCaptureLocketSO LocketSO => _locketSO;
    }
}

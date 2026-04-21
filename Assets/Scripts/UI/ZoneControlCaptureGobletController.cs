using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGobletController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGobletSO _gobletSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGobletFilled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _pourLabel;
        [SerializeField] private Text       _gobletLabel;
        [SerializeField] private Slider     _pourBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleGobletFilledDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleGobletFilledDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGobletFilled?.RegisterCallback(_handleGobletFilledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGobletFilled?.UnregisterCallback(_handleGobletFilledDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_gobletSO == null) return;
            int bonus = _gobletSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_gobletSO == null) return;
            _gobletSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _gobletSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_gobletSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_pourLabel != null)
                _pourLabel.text = $"Pours: {_gobletSO.Pours}/{_gobletSO.PoursNeeded}";

            if (_gobletLabel != null)
                _gobletLabel.text = $"Goblets: {_gobletSO.GobletCount}";

            if (_pourBar != null)
                _pourBar.value = _gobletSO.PourProgress;
        }

        public ZoneControlCaptureGobletSO GobletSO => _gobletSO;
    }
}

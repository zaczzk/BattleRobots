using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureHammerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureHammerSO _hammerSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHammerForged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _strikeLabel;
        [SerializeField] private Text       _forgeLabel;
        [SerializeField] private Slider     _strikeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleHammerForgedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleHammerForgedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHammerForged?.RegisterCallback(_handleHammerForgedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHammerForged?.UnregisterCallback(_handleHammerForgedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_hammerSO == null) return;
            int bonus = _hammerSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_hammerSO == null) return;
            _hammerSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _hammerSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_hammerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_strikeLabel != null)
                _strikeLabel.text = $"Strikes: {_hammerSO.Strikes}/{_hammerSO.StrikesNeeded}";

            if (_forgeLabel != null)
                _forgeLabel.text = $"Forges: {_hammerSO.ForgeCount}";

            if (_strikeBar != null)
                _strikeBar.value = _hammerSO.StrikeProgress;
        }

        public ZoneControlCaptureHammerSO HammerSO => _hammerSO;
    }
}

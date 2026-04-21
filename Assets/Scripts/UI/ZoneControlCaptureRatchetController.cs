using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRatchetController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRatchetSO _ratchetSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRatchetAdvanced;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _clickLabel;
        [SerializeField] private Text       _advanceLabel;
        [SerializeField] private Slider     _clickBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleAdvancedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleAdvancedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRatchetAdvanced?.RegisterCallback(_handleAdvancedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRatchetAdvanced?.UnregisterCallback(_handleAdvancedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_ratchetSO == null) return;
            int bonus = _ratchetSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_ratchetSO == null) return;
            _ratchetSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _ratchetSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_ratchetSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_clickLabel != null)
                _clickLabel.text = $"Clicks: {_ratchetSO.Clicks}/{_ratchetSO.ClicksNeeded}";

            if (_advanceLabel != null)
                _advanceLabel.text = $"Advances: {_ratchetSO.AdvanceCount}";

            if (_clickBar != null)
                _clickBar.value = _ratchetSO.ClickProgress;
        }

        public ZoneControlCaptureRatchetSO RatchetSO => _ratchetSO;
    }
}

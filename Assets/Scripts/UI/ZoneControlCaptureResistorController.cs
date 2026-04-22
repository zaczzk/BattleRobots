using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureResistorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureResistorSO _resistorSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onResistorBlocked;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _ohmLabel;
        [SerializeField] private Text       _blockLabel;
        [SerializeField] private Slider     _ohmBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBlockedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleBlockedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onResistorBlocked?.RegisterCallback(_handleBlockedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onResistorBlocked?.UnregisterCallback(_handleBlockedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_resistorSO == null) return;
            int bonus = _resistorSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_resistorSO == null) return;
            _resistorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _resistorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_resistorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_ohmLabel != null)
                _ohmLabel.text = $"Ohms: {_resistorSO.Ohms}/{_resistorSO.OhmsNeeded}";

            if (_blockLabel != null)
                _blockLabel.text = $"Blocks: {_resistorSO.BlockCount}";

            if (_ohmBar != null)
                _ohmBar.value = _resistorSO.OhmProgress;
        }

        public ZoneControlCaptureResistorSO ResistorSO => _resistorSO;
    }
}

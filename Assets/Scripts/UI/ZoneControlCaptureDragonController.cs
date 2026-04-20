using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDragonController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDragonSO _dragonSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onHoardFilled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _goldLabel;
        [SerializeField] private Text       _hoardLabel;
        [SerializeField] private Slider     _goldBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleHoardDelegate;

        private void Awake()
        {
            _handlePlayerDelegate      = HandlePlayerCaptured;
            _handleBotDelegate         = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleHoardDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onHoardFilled?.RegisterCallback(_handleHoardDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onHoardFilled?.UnregisterCallback(_handleHoardDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_dragonSO == null) return;
            int bonus = _dragonSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_dragonSO == null) return;
            _dragonSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _dragonSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_dragonSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_goldLabel != null)
                _goldLabel.text = $"Gold: {_dragonSO.Gold}/{_dragonSO.HoardNeeded}";

            if (_hoardLabel != null)
                _hoardLabel.text = $"Hoards: {_dragonSO.HoardCount}";

            if (_goldBar != null)
                _goldBar.value = _dragonSO.GoldProgress;
        }

        public ZoneControlCaptureDragonSO DragonSO => _dragonSO;
    }
}

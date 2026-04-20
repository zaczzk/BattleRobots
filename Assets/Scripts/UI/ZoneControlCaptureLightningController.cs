using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLightningController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLightningSO _lightningSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLightningStruck;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _chargeLabel;
        [SerializeField] private Text       _strikeLabel;
        [SerializeField] private Slider     _chargeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleStruckDelegate;

        private void Awake()
        {
            _handlePlayerDelegate      = HandlePlayerCaptured;
            _handleBotDelegate         = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleStruckDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLightningStruck?.RegisterCallback(_handleStruckDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLightningStruck?.UnregisterCallback(_handleStruckDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_lightningSO == null) return;
            int bonus = _lightningSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_lightningSO == null) return;
            _lightningSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _lightningSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_lightningSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_chargeLabel != null)
                _chargeLabel.text = $"Charges: {_lightningSO.Charges}/{_lightningSO.ChargesNeeded}";

            if (_strikeLabel != null)
                _strikeLabel.text = $"Strikes: {_lightningSO.StrikeCount}";

            if (_chargeBar != null)
                _chargeBar.value = _lightningSO.ChargeProgress;
        }

        public ZoneControlCaptureLightningSO LightningSO => _lightningSO;
    }
}

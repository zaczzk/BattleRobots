using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSanctuaryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSanctuarySO _sanctuarySO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSanctuarySealed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _chargeLabel;
        [SerializeField] private Text       _sanctuaryLabel;
        [SerializeField] private Slider     _chargeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSealedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSealedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSanctuarySealed?.RegisterCallback(_handleSealedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSanctuarySealed?.UnregisterCallback(_handleSealedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_sanctuarySO == null) return;
            int bonus = _sanctuarySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_sanctuarySO == null) return;
            _sanctuarySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _sanctuarySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_sanctuarySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_chargeLabel != null)
                _chargeLabel.text = $"Charges: {_sanctuarySO.Charges}/{_sanctuarySO.ChargesNeeded}";

            if (_sanctuaryLabel != null)
                _sanctuaryLabel.text = $"Sanctuaries: {_sanctuarySO.SanctuaryCount}";

            if (_chargeBar != null)
                _chargeBar.value = _sanctuarySO.ChargeProgress;
        }

        public ZoneControlCaptureSanctuarySO SanctuarySO => _sanctuarySO;
    }
}

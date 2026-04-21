using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCondenserController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCondenserSO _condenserSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCondenserCharged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _plateLabel;
        [SerializeField] private Text       _chargeLabel;
        [SerializeField] private Slider     _plateBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleChargedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleChargedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCondenserCharged?.RegisterCallback(_handleChargedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCondenserCharged?.UnregisterCallback(_handleChargedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_condenserSO == null) return;
            int bonus = _condenserSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_condenserSO == null) return;
            _condenserSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _condenserSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_condenserSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_plateLabel != null)
                _plateLabel.text = $"Plates: {_condenserSO.Plates}/{_condenserSO.PlatesNeeded}";

            if (_chargeLabel != null)
                _chargeLabel.text = $"Charges: {_condenserSO.ChargeCount}";

            if (_plateBar != null)
                _plateBar.value = _condenserSO.PlateProgress;
        }

        public ZoneControlCaptureCondenserSO CondenserSO => _condenserSO;
    }
}

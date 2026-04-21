using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTalismanController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTalismanSO _talismanSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTalismanActivated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _chargeLabel;
        [SerializeField] private Text       _activationLabel;
        [SerializeField] private Slider     _chargeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTalismanActivatedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate            = HandlePlayerCaptured;
            _handleBotDelegate               = HandleBotCaptured;
            _handleMatchStartedDelegate      = HandleMatchStarted;
            _handleTalismanActivatedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTalismanActivated?.RegisterCallback(_handleTalismanActivatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTalismanActivated?.UnregisterCallback(_handleTalismanActivatedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_talismanSO == null) return;
            int bonus = _talismanSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_talismanSO == null) return;
            _talismanSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _talismanSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_talismanSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_chargeLabel != null)
                _chargeLabel.text = $"Charges: {_talismanSO.Charges}/{_talismanSO.ChargesNeeded}";

            if (_activationLabel != null)
                _activationLabel.text = $"Activations: {_talismanSO.ActivationCount}";

            if (_chargeBar != null)
                _chargeBar.value = _talismanSO.ChargeProgress;
        }

        public ZoneControlCaptureTalismanSO TalismanSO => _talismanSO;
    }
}

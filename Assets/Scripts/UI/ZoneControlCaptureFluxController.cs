using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFluxController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFluxSO _fluxSO;
        [SerializeField] private PlayerWalletSO           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFlux;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _fluxLabel;
        [SerializeField] private Text       _fluxCountLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFluxDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFluxDelegate         = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFlux?.RegisterCallback(_handleFluxDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFlux?.UnregisterCallback(_handleFluxDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_fluxSO == null) return;
            int bonus = _fluxSO.RecordPlayerCapture(UnityEngine.Time.time);
            if (bonus > 0)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_fluxSO == null) return;
            _fluxSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _fluxSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_fluxSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_fluxLabel != null)
                _fluxLabel.text = _fluxSO.HasPriorCapture ? "FLUX READY!" : "Waiting...";

            if (_fluxCountLabel != null)
                _fluxCountLabel.text = $"Flux Bonus: {_fluxSO.TotalBonusAwarded}";
        }

        public ZoneControlCaptureFluxSO FluxSO => _fluxSO;
    }
}

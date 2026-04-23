using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDerivationController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDerivationSO _derivationSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDerivationComputed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _operandLabel;
        [SerializeField] private Text       _derivationLabel;
        [SerializeField] private Slider     _operandBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleComputedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleComputedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDerivationComputed?.RegisterCallback(_handleComputedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDerivationComputed?.UnregisterCallback(_handleComputedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_derivationSO == null) return;
            int bonus = _derivationSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_derivationSO == null) return;
            _derivationSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _derivationSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_derivationSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_operandLabel != null)
                _operandLabel.text = $"Operands: {_derivationSO.Operands}/{_derivationSO.OperandsNeeded}";

            if (_derivationLabel != null)
                _derivationLabel.text = $"Derivations: {_derivationSO.DerivationCount}";

            if (_operandBar != null)
                _operandBar.value = _derivationSO.OperandProgress;
        }

        public ZoneControlCaptureDerivationSO DerivationSO => _derivationSO;
    }
}

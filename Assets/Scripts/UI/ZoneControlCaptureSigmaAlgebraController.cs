using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSigmaAlgebraController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSigmaAlgebraSO _sigmaAlgebraSO;
        [SerializeField] private PlayerWallet                      _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSigmaAlgebraMeasured;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _setLabel;
        [SerializeField] private Text       _measureLabel;
        [SerializeField] private Slider     _setBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMeasuredDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMeasuredDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSigmaAlgebraMeasured?.RegisterCallback(_handleMeasuredDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSigmaAlgebraMeasured?.UnregisterCallback(_handleMeasuredDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_sigmaAlgebraSO == null) return;
            int bonus = _sigmaAlgebraSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_sigmaAlgebraSO == null) return;
            _sigmaAlgebraSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _sigmaAlgebraSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_sigmaAlgebraSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_setLabel != null)
                _setLabel.text = $"Sets: {_sigmaAlgebraSO.Sets}/{_sigmaAlgebraSO.SetsNeeded}";

            if (_measureLabel != null)
                _measureLabel.text = $"Measures: {_sigmaAlgebraSO.MeasureCount}";

            if (_setBar != null)
                _setBar.value = _sigmaAlgebraSO.SetProgress;
        }

        public ZoneControlCaptureSigmaAlgebraSO SigmaAlgebraSO => _sigmaAlgebraSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureExponentialController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureExponentialSO _exponentialSO;
        [SerializeField] private PlayerWallet                     _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onExponentRaised;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _baseLabel;
        [SerializeField] private Text       _exponentLabel;
        [SerializeField] private Slider     _baseBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRaisedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRaisedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onExponentRaised?.RegisterCallback(_handleRaisedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onExponentRaised?.UnregisterCallback(_handleRaisedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_exponentialSO == null) return;
            int bonus = _exponentialSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_exponentialSO == null) return;
            _exponentialSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _exponentialSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_exponentialSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_baseLabel != null)
                _baseLabel.text = $"Bases: {_exponentialSO.Bases}/{_exponentialSO.BasesNeeded}";

            if (_exponentLabel != null)
                _exponentLabel.text = $"Exponents: {_exponentialSO.ExponentCount}";

            if (_baseBar != null)
                _baseBar.value = _exponentialSO.BaseProgress;
        }

        public ZoneControlCaptureExponentialSO ExponentialSO => _exponentialSO;
    }
}

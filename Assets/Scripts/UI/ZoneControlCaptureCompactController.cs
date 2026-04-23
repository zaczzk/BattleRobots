using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCompactController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCompactSO _compactSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCompactObjectFormed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _factorLabel;
        [SerializeField] private Text       _compactLabel;
        [SerializeField] private Slider     _factorBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFormedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFormedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCompactObjectFormed?.RegisterCallback(_handleFormedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCompactObjectFormed?.UnregisterCallback(_handleFormedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_compactSO == null) return;
            int bonus = _compactSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_compactSO == null) return;
            _compactSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _compactSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_compactSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_factorLabel != null)
                _factorLabel.text = $"Factors: {_compactSO.Factors}/{_compactSO.FactorsNeeded}";

            if (_compactLabel != null)
                _compactLabel.text = $"Compact: {_compactSO.CompactCount}";

            if (_factorBar != null)
                _factorBar.value = _compactSO.FactorProgress;
        }

        public ZoneControlCaptureCompactSO CompactSO => _compactSO;
    }
}

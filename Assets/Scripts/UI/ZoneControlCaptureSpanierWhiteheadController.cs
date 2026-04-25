using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSpanierWhiteheadController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSpanierWhiteheadSO _spanierWhiteheadSO;
        [SerializeField] private PlayerWallet                          _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSpanierWhiteheadDualized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _dualPairLabel;
        [SerializeField] private Text       _dualizeLabel;
        [SerializeField] private Slider     _dualPairBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDualizedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDualizedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSpanierWhiteheadDualized?.RegisterCallback(_handleDualizedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSpanierWhiteheadDualized?.UnregisterCallback(_handleDualizedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_spanierWhiteheadSO == null) return;
            int bonus = _spanierWhiteheadSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_spanierWhiteheadSO == null) return;
            _spanierWhiteheadSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _spanierWhiteheadSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_spanierWhiteheadSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_dualPairLabel != null)
                _dualPairLabel.text = $"Dual Pairs: {_spanierWhiteheadSO.DualPairs}/{_spanierWhiteheadSO.DualPairsNeeded}";

            if (_dualizeLabel != null)
                _dualizeLabel.text = $"Dualizations: {_spanierWhiteheadSO.DualizationCount}";

            if (_dualPairBar != null)
                _dualPairBar.value = _spanierWhiteheadSO.DualPairProgress;
        }

        public ZoneControlCaptureSpanierWhiteheadSO SpanierWhiteheadSO => _spanierWhiteheadSO;
    }
}

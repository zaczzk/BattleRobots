using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureNashEquilibriumController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureNashEquilibriumSO _nashSO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNashEquilibriumReached;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _strategyPairLabel;
        [SerializeField] private Text       _equilibriumLabel;
        [SerializeField] private Slider     _strategyPairBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleNashDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleNashDelegate         = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNashEquilibriumReached?.RegisterCallback(_handleNashDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNashEquilibriumReached?.UnregisterCallback(_handleNashDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_nashSO == null) return;
            int bonus = _nashSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_nashSO == null) return;
            _nashSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _nashSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_nashSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_strategyPairLabel != null)
                _strategyPairLabel.text = $"Strategy Pairs: {_nashSO.StrategyPairs}/{_nashSO.StrategyPairsNeeded}";

            if (_equilibriumLabel != null)
                _equilibriumLabel.text = $"Equilibria: {_nashSO.EquilibriumCount}";

            if (_strategyPairBar != null)
                _strategyPairBar.value = _nashSO.StrategyPairProgress;
        }

        public ZoneControlCaptureNashEquilibriumSO NashSO => _nashSO;
    }
}

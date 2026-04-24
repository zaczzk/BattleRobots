using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCyclicCohomologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCyclicCohomologySO _cyclicSO;
        [SerializeField] private PlayerWallet                          _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCyclicCohomologyTraced;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _cycleLabel;
        [SerializeField] private Text       _traceLabel;
        [SerializeField] private Slider     _cycleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTracedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleTracedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCyclicCohomologyTraced?.RegisterCallback(_handleTracedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCyclicCohomologyTraced?.UnregisterCallback(_handleTracedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cyclicSO == null) return;
            int bonus = _cyclicSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cyclicSO == null) return;
            _cyclicSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cyclicSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cyclicSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_cycleLabel != null)
                _cycleLabel.text = $"Cycles: {_cyclicSO.Cycles}/{_cyclicSO.CyclesNeeded}";

            if (_traceLabel != null)
                _traceLabel.text = $"Traces: {_cyclicSO.TraceCount}";

            if (_cycleBar != null)
                _cycleBar.value = _cyclicSO.CycleProgress;
        }

        public ZoneControlCaptureCyclicCohomologySO CyclicSO => _cyclicSO;
    }
}

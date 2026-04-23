using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTracedController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTracedSO _tracedSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTraced;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _loopLabel;
        [SerializeField] private Text       _traceLabel;
        [SerializeField] private Slider     _loopBar;
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
            _onTraced?.RegisterCallback(_handleTracedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTraced?.UnregisterCallback(_handleTracedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_tracedSO == null) return;
            int bonus = _tracedSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_tracedSO == null) return;
            _tracedSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _tracedSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_tracedSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_loopLabel != null)
                _loopLabel.text = $"Loops: {_tracedSO.Loops}/{_tracedSO.LoopsNeeded}";

            if (_traceLabel != null)
                _traceLabel.text = $"Traces: {_tracedSO.TraceCount}";

            if (_loopBar != null)
                _loopBar.value = _tracedSO.LoopProgress;
        }

        public ZoneControlCaptureTracedSO TracedSO => _tracedSO;
    }
}

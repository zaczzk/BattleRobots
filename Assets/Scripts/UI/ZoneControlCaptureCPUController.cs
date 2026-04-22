using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCPUController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCPUSO _cpuSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCPUCycled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _cycleLabel;
        [SerializeField] private Text       _cpuLabel;
        [SerializeField] private Slider     _cycleBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCycledDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCycledDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCPUCycled?.RegisterCallback(_handleCycledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCPUCycled?.UnregisterCallback(_handleCycledDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cpuSO == null) return;
            int bonus = _cpuSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cpuSO == null) return;
            _cpuSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cpuSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cpuSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_cycleLabel != null)
                _cycleLabel.text = $"Cycles: {_cpuSO.Cycles}/{_cpuSO.CyclesNeeded}";

            if (_cpuLabel != null)
                _cpuLabel.text = $"CPU Runs: {_cpuSO.CycleCount}";

            if (_cycleBar != null)
                _cycleBar.value = _cpuSO.CycleProgress;
        }

        public ZoneControlCaptureCPUSO CPUSO => _cpuSO;
    }
}

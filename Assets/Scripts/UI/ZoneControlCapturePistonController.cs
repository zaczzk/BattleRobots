using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePistonController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePistonSO _pistonSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPistonCycled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _strokeLabel;
        [SerializeField] private Text       _cycleLabel;
        [SerializeField] private Slider     _strokeBar;
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
            _onPistonCycled?.RegisterCallback(_handleCycledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPistonCycled?.UnregisterCallback(_handleCycledDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_pistonSO == null) return;
            int bonus = _pistonSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_pistonSO == null) return;
            _pistonSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _pistonSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_pistonSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_strokeLabel != null)
                _strokeLabel.text = $"Strokes: {_pistonSO.Strokes}/{_pistonSO.StrokesNeeded}";

            if (_cycleLabel != null)
                _cycleLabel.text = $"Cycles: {_pistonSO.CycleCount}";

            if (_strokeBar != null)
                _strokeBar.value = _pistonSO.StrokeProgress;
        }

        public ZoneControlCapturePistonSO PistonSO => _pistonSO;
    }
}

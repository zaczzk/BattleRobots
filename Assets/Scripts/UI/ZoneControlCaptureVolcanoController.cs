using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureVolcanoController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureVolcanoSO _volcanoSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onEruption;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _pressureLabel;
        [SerializeField] private Text       _eruptionLabel;
        [SerializeField] private Slider     _pressureBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleEruptionDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleEruptionDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onEruption?.RegisterCallback(_handleEruptionDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onEruption?.UnregisterCallback(_handleEruptionDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_volcanoSO == null) return;
            int bonus = _volcanoSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_volcanoSO == null) return;
            _volcanoSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _volcanoSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_volcanoSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_pressureLabel != null)
                _pressureLabel.text = $"Pressure: {_volcanoSO.Pressure:F0}%";

            if (_eruptionLabel != null)
                _eruptionLabel.text = $"Eruptions: {_volcanoSO.EruptionCount}";

            if (_pressureBar != null)
                _pressureBar.value = _volcanoSO.PressureProgress;
        }

        public ZoneControlCaptureVolcanoSO VolcanoSO => _volcanoSO;
    }
}

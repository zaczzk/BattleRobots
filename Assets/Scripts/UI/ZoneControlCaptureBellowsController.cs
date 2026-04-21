using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBellowsController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBellowsSO _bellowsSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBellowsBlasted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _pumpLabel;
        [SerializeField] private Text       _blastLabel;
        [SerializeField] private Slider     _pumpBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBlastedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleBlastedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBellowsBlasted?.RegisterCallback(_handleBlastedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBellowsBlasted?.UnregisterCallback(_handleBlastedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_bellowsSO == null) return;
            int bonus = _bellowsSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_bellowsSO == null) return;
            _bellowsSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _bellowsSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_bellowsSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_pumpLabel != null)
                _pumpLabel.text = $"Pumps: {_bellowsSO.Pumps}/{_bellowsSO.PumpsNeeded}";

            if (_blastLabel != null)
                _blastLabel.text = $"Blasts: {_bellowsSO.BlastCount}";

            if (_pumpBar != null)
                _pumpBar.value = _bellowsSO.PumpProgress;
        }

        public ZoneControlCaptureBellowsSO BellowsSO => _bellowsSO;
    }
}

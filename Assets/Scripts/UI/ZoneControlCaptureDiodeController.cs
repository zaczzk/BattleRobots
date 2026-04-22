using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDiodeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDiodeSO _diodeSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDiodeConducted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _junctionLabel;
        [SerializeField] private Text       _conductionLabel;
        [SerializeField] private Slider     _junctionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleConductedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleConductedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDiodeConducted?.RegisterCallback(_handleConductedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDiodeConducted?.UnregisterCallback(_handleConductedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_diodeSO == null) return;
            int bonus = _diodeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_diodeSO == null) return;
            _diodeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _diodeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_diodeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_junctionLabel != null)
                _junctionLabel.text = $"Junctions: {_diodeSO.Junctions}/{_diodeSO.JunctionsNeeded}";

            if (_conductionLabel != null)
                _conductionLabel.text = $"Conductions: {_diodeSO.ConductionCount}";

            if (_junctionBar != null)
                _junctionBar.value = _diodeSO.JunctionProgress;
        }

        public ZoneControlCaptureDiodeSO DiodeSO => _diodeSO;
    }
}

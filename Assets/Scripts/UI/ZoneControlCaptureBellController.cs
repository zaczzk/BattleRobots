using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBellController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBellSO _bellSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBellTolled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _ringLabel;
        [SerializeField] private Text       _tollLabel;
        [SerializeField] private Slider     _ringBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBellTolledDelegate;

        private void Awake()
        {
            _handlePlayerDelegate      = HandlePlayerCaptured;
            _handleBotDelegate         = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleBellTolledDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBellTolled?.RegisterCallback(_handleBellTolledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBellTolled?.UnregisterCallback(_handleBellTolledDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_bellSO == null) return;
            int bonus = _bellSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_bellSO == null) return;
            _bellSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _bellSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_bellSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_ringLabel != null)
                _ringLabel.text = $"Rings: {_bellSO.Rings}/{_bellSO.RingsNeeded}";

            if (_tollLabel != null)
                _tollLabel.text = $"Tolls: {_bellSO.TollCount}";

            if (_ringBar != null)
                _ringBar.value = _bellSO.RingProgress;
        }

        public ZoneControlCaptureBellSO BellSO => _bellSO;
    }
}

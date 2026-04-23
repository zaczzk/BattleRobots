using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureToposController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureToposSO _toposSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onToposSieved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _pointLabel;
        [SerializeField] private Text       _sieveLabel;
        [SerializeField] private Slider     _pointBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSievedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleSievedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onToposSieved?.RegisterCallback(_handleSievedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onToposSieved?.UnregisterCallback(_handleSievedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_toposSO == null) return;
            int bonus = _toposSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_toposSO == null) return;
            _toposSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _toposSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_toposSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_pointLabel != null)
                _pointLabel.text = $"Points: {_toposSO.Points}/{_toposSO.PointsNeeded}";

            if (_sieveLabel != null)
                _sieveLabel.text = $"Sieves: {_toposSO.SieveCount}";

            if (_pointBar != null)
                _pointBar.value = _toposSO.PointProgress;
        }

        public ZoneControlCaptureToposSO ToposSO => _toposSO;
    }
}

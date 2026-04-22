using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSearchController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSearchSO _searchSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTargetFound;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _probeLabel;
        [SerializeField] private Text       _findLabel;
        [SerializeField] private Slider     _probeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTargetFoundDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleTargetFoundDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTargetFound?.RegisterCallback(_handleTargetFoundDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTargetFound?.UnregisterCallback(_handleTargetFoundDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_searchSO == null) return;
            int bonus = _searchSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_searchSO == null) return;
            _searchSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _searchSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_searchSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_probeLabel != null)
                _probeLabel.text = $"Probes: {_searchSO.Probes}/{_searchSO.ProbesNeeded}";

            if (_findLabel != null)
                _findLabel.text = $"Finds: {_searchSO.FindCount}";

            if (_probeBar != null)
                _probeBar.value = _searchSO.ProbeProgress;
        }

        public ZoneControlCaptureSearchSO SearchSO => _searchSO;
    }
}

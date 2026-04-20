using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureConstellationController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureConstellationSO _constellationSO;
        [SerializeField] private PlayerWallet                      _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onConstellationFormed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _starLabel;
        [SerializeField] private Text       _constellationLabel;
        [SerializeField] private Slider     _starBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleConstellationDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleConstellationDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onConstellationFormed?.RegisterCallback(_handleConstellationDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onConstellationFormed?.UnregisterCallback(_handleConstellationDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_constellationSO == null) return;
            int prev  = _constellationSO.ConstellationCount;
            int bonus = _constellationSO.RecordPlayerCapture();
            if (_constellationSO.ConstellationCount > prev) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_constellationSO == null) return;
            _constellationSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _constellationSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_constellationSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_starLabel != null)
                _starLabel.text = $"Stars: {_constellationSO.ActiveStars}/{_constellationSO.StarsNeeded}";

            if (_constellationLabel != null)
                _constellationLabel.text = $"Constellations: {_constellationSO.ConstellationCount}";

            if (_starBar != null)
                _starBar.value = _constellationSO.StarProgress;
        }

        public ZoneControlCaptureConstellationSO ConstellationSO => _constellationSO;
    }
}

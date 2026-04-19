using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureOrbitController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureOrbitSO _orbitSO;
        [SerializeField] private PlayerWalletSO            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onOrbit;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _orbitLabel;
        [SerializeField] private Text       _orbitsLabel;
        [SerializeField] private Slider     _orbitBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleOrbitDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleOrbitDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onOrbit?.RegisterCallback(_handleOrbitDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onOrbit?.UnregisterCallback(_handleOrbitDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_orbitSO == null) return;
            int prevOrbits = _orbitSO.OrbitCount;
            _orbitSO.RecordPlayerCapture();
            if (_orbitSO.OrbitCount > prevOrbits)
                _wallet?.AddFunds(_orbitSO.BonusPerOrbit);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_orbitSO == null) return;
            _orbitSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _orbitSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_orbitSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_orbitLabel != null)
                _orbitLabel.text = $"Orbit: {_orbitSO.CurrentOrbit}/{_orbitSO.OrbitTarget}";

            if (_orbitsLabel != null)
                _orbitsLabel.text = $"Orbits: {_orbitSO.OrbitCount}";

            if (_orbitBar != null)
                _orbitBar.value = _orbitSO.OrbitProgress;
        }

        public ZoneControlCaptureOrbitSO OrbitSO => _orbitSO;
    }
}

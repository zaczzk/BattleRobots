using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlTurboCaptureController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlTurboCaptureSO _turboSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTurbo;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _turboLabel;
        [SerializeField] private Text       _nextTurboLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTurboDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleTurboDelegate        = HandleTurbo;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTurbo?.RegisterCallback(_handleTurboDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTurbo?.UnregisterCallback(_handleTurboDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_turboSO == null) return;
            int prevTurbo = _turboSO.TurboCount;
            _turboSO.RecordCapture();
            if (_turboSO.TurboCount > prevTurbo)
                _wallet?.AddFunds(_turboSO.BonusPerTurbo);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _turboSO?.Reset();
            Refresh();
        }

        private void HandleTurbo()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_turboSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_turboLabel != null)
                _turboLabel.text = $"Turbo: {_turboSO.TurboCount}";

            if (_nextTurboLabel != null)
                _nextTurboLabel.text = $"Next: {_turboSO.NextTurboIn} caps";
        }

        public ZoneControlTurboCaptureSO TurboSO => _turboSO;
    }
}

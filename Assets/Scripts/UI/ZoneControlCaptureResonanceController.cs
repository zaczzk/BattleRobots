using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureResonanceController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureResonanceSO _resonanceSO;
        [SerializeField] private PlayerWalletSO                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onResonance;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _resonanceLabel;
        [SerializeField] private Text       _resonancesLabel;
        [SerializeField] private Slider     _resonanceBar;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleResonanceDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleResonanceDelegate    = HandleResonance;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onResonance?.RegisterCallback(_handleResonanceDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onResonance?.UnregisterCallback(_handleResonanceDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_resonanceSO == null) return;
            _resonanceSO.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _resonanceSO?.Reset();
            Refresh();
        }

        private void HandleResonance()
        {
            if (_resonanceSO == null) return;
            _wallet?.AddFunds(_resonanceSO.BonusPerResonance);
            Refresh();
        }

        public void Refresh()
        {
            if (_resonanceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_resonanceLabel != null)
                _resonanceLabel.text = $"Resonance: {_resonanceSO.CaptureCount}/{_resonanceSO.ResonanceTarget}";

            if (_resonancesLabel != null)
                _resonancesLabel.text = $"Resonances: {_resonanceSO.ResonanceCount}";

            if (_resonanceBar != null)
                _resonanceBar.value = _resonanceSO.ResonanceProgress;
        }

        public ZoneControlCaptureResonanceSO ResonanceSO => _resonanceSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureWaveController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureWaveSO _waveSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onWaveScored;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _waveLabel;
        [SerializeField] private Text       _bestWaveLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _handleWaveScoredDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMatchEndedDelegate   = HandleMatchEnded;
            _handleWaveScoredDelegate   = HandleWaveScored;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onWaveScored?.RegisterCallback(_handleWaveScoredDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onWaveScored?.UnregisterCallback(_handleWaveScoredDelegate);
        }

        private void Update()
        {
            if (_waveSO == null || !_waveSO.IsWaveActive) return;
            _waveSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleZoneCaptured()
        {
            if (_waveSO == null) return;
            _waveSO.RecordCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _waveSO?.Reset();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            _waveSO?.Reset();
            Refresh();
        }

        private void HandleWaveScored()
        {
            if (_waveSO == null) return;
            _wallet?.AddFunds(_waveSO.LastWaveBonus);
            Refresh();
        }

        public void Refresh()
        {
            if (_waveSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_waveLabel != null)
                _waveLabel.text = $"Wave: {_waveSO.CurrentWaveCaptures} captures";

            if (_bestWaveLabel != null)
                _bestWaveLabel.text = $"Best Wave: {_waveSO.BestWaveCaptures}";
        }

        public ZoneControlCaptureWaveSO WaveSO => _waveSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBurstTargetController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBurstTargetSO _burstTargetSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBurstTargetMet;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _progressLabel;
        [SerializeField] private Text       _burstsLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBurstTargetMetDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate  = HandleZoneCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleBurstTargetMetDelegate = HandleBurstTargetMet;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBurstTargetMet?.RegisterCallback(_handleBurstTargetMetDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBurstTargetMet?.UnregisterCallback(_handleBurstTargetMetDelegate);
        }

        private void Update()
        {
            _burstTargetSO?.Tick(Time.time);
        }

        private void HandleZoneCaptured()
        {
            if (_burstTargetSO == null) return;
            _burstTargetSO.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _burstTargetSO?.Reset();
            Refresh();
        }

        private void HandleBurstTargetMet()
        {
            _wallet?.AddFunds(_burstTargetSO?.BurstReward ?? 0);
            Refresh();
        }

        public void Refresh()
        {
            if (_burstTargetSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_progressLabel != null)
                _progressLabel.text = $"Burst: {_burstTargetSO.CaptureCount}/{_burstTargetSO.BurstTargetCount}";

            if (_burstsLabel != null)
                _burstsLabel.text = $"Bursts: {_burstTargetSO.BurstsMet}";
        }

        public ZoneControlCaptureBurstTargetSO BurstTargetSO => _burstTargetSO;
    }
}

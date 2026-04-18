using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureConsistencyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureConsistencySO _consistencySO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onConsistentCapture;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _avgGapLabel;
        [SerializeField] private Text       _consistentLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleConsistentCaptureDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate      = HandleZoneCaptured;
            _handleMatchStartedDelegate      = HandleMatchStarted;
            _handleConsistentCaptureDelegate = HandleConsistentCapture;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onConsistentCapture?.RegisterCallback(_handleConsistentCaptureDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onConsistentCapture?.UnregisterCallback(_handleConsistentCaptureDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_consistencySO == null) return;
            _consistencySO.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _consistencySO?.Reset();
            Refresh();
        }

        private void HandleConsistentCapture() => Refresh();

        public void Refresh()
        {
            if (_consistencySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_avgGapLabel != null)
                _avgGapLabel.text = $"Avg Gap: {_consistencySO.AverageGap:F1}s";

            if (_consistentLabel != null)
                _consistentLabel.text = $"Consistent: {_consistencySO.ConsistentCaptures}";
        }

        public ZoneControlCaptureConsistencySO ConsistencySO => _consistencySO;
    }
}

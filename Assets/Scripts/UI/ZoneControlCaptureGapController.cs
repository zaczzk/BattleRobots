using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGapController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGapSO _gapSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFastCapture;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _gapLabel;
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFastCapture?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFastCapture?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_gapSO == null) return;
            _gapSO.RecordCapture(Time.time);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _gapSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_gapSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_gapLabel != null)
                _gapLabel.text = _gapSO.HasFirstCapture
                    ? $"Gap: {_gapSO.LastGap:F1}s"
                    : "Gap: --";

            if (_statusLabel != null)
            {
                bool isFast = _gapSO.HasFirstCapture &&
                              _gapSO.LastGap <= _gapSO.FastGapThreshold &&
                              _gapSO.FastCaptureCount > 0;
                _statusLabel.text = isFast ? "FAST!" : "Normal";
            }
        }

        public ZoneControlCaptureGapSO GapSO => _gapSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that computes and displays the match quality score
    /// from <see cref="ZoneControlMatchQualitySO"/> on match-end.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchQualityController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchQualitySO    _qualitySO;
        [SerializeField] private ZoneControlSessionSummarySO  _summarySO;
        [SerializeField] private ZoneCapturePaceTrackerSO      _trackerSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onQualityComputed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _qualityLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
            _refreshDelegate          = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onQualityComputed?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onQualityComputed?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleMatchEnded()
        {
            if (_qualitySO == null) { Refresh(); return; }
            int   zones  = _summarySO?.TotalZonesCaptured ?? 0;
            float pace   = _trackerSO?.GetCapturesPerMinute(Time.time) ?? 0f;
            _qualitySO.ComputeQuality(zones, pace, 0);
            Refresh();
        }

        public void Refresh()
        {
            if (_qualitySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_qualityLabel != null)
                _qualityLabel.text = $"Quality: {_qualitySO.LastQuality}/100";
        }

        public ZoneControlMatchQualitySO   QualitySO => _qualitySO;
        public ZoneControlSessionSummarySO SummarySO => _summarySO;
        public ZoneCapturePaceTrackerSO    TrackerSO => _trackerSO;
    }
}

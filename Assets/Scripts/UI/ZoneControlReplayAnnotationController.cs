using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives the replay-annotation panel.
    /// Auto-adds a "Zone Captured" annotation on each zone-capture event
    /// and resets on match start.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlReplayAnnotationController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlReplayAnnotationSO _annotationSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onAnnotationAdded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _countLabel;
        [SerializeField] private Text       _latestLabel;
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
            _onAnnotationAdded?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onAnnotationAdded?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneCaptured()
        {
            _annotationSO?.AddAnnotation(Time.time, "Zone Captured");
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _annotationSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_annotationSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_countLabel != null)
                _countLabel.text = $"Annotations: {_annotationSO.AnnotationCount}";

            if (_latestLabel != null)
            {
                var entries = _annotationSO.GetAnnotations();
                _latestLabel.text = entries.Count > 0
                    ? entries[entries.Count - 1].Note
                    : string.Empty;
            }
        }

        public ZoneControlReplayAnnotationSO AnnotationSO => _annotationSO;
    }
}

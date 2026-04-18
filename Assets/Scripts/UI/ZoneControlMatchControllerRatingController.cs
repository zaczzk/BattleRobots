using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchControllerRatingController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchControllerRatingSO _ratingSO;

        [Header("Input Sources (optional)")]
        [SerializeField] private ZoneControlZoneHoldRatioSO        _holdRatioSO;
        [SerializeField] private ZoneControlCaptureEfficiencySO    _efficiencySO;
        [SerializeField] private ZoneControlMatchFinisherSO        _finisherSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRatingComputed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _ratingLabel;
        [SerializeField] private Text       _gradeLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchEndedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRatingComputedDelegate;

        private void Awake()
        {
            _handleMatchEndedDelegate    = HandleMatchEnded;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleRatingComputedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onRatingComputed?.RegisterCallback(_handleRatingComputedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRatingComputed?.UnregisterCallback(_handleRatingComputedDelegate);
        }

        private void HandleMatchEnded()
        {
            if (_ratingSO == null) return;

            float holdRatio  = _holdRatioSO?.HoldRatio ?? 0f;
            float efficiency = _efficiencySO?.Efficiency ?? 0f;
            int   leadDelta  = _finisherSO != null
                ? (_finisherSO.LastBonus > 0 ? _ratingSO.MaxLeadDelta : 0)
                : 0;

            _ratingSO.ComputeRating(holdRatio, efficiency, leadDelta);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _ratingSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_ratingSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_ratingLabel != null)
                _ratingLabel.text = $"Rating: {_ratingSO.LastRating}/100";

            if (_gradeLabel != null)
                _gradeLabel.text = ZoneControlMatchControllerRatingSO.GetGradeLabel(_ratingSO.LastRating);
        }

        public ZoneControlMatchControllerRatingSO RatingSO => _ratingSO;
    }
}

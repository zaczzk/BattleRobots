using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives <see cref="ZoneControlZoneOwnershipRatingSO"/>
    /// via a periodic Update tick and displays a "Control: N%" label with a fill bar.
    ///
    /// <c>_onMatchStarted</c>: Reset + Refresh.
    /// <c>_onMatchEnded</c>: final Refresh only.
    /// <c>_onRatingUpdated</c>: Refresh.
    /// Update: RecordControlTick from <c>_dominanceSO</c> (if available) every
    /// <c>_tickInterval</c> seconds.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneOwnershipRatingController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneOwnershipRatingSO _ownershipRatingSO;
        [SerializeField] private ZoneDominanceSO                  _dominanceSO;

        [Header("Tick Interval (seconds)")]
        [Min(0.1f)]
        [SerializeField] private float _tickInterval = 1f;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onRatingUpdated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _ratingLabel;
        [SerializeField] private Slider     _ratingBar;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        private bool  _matchRunning;
        private float _tickTimer;

        private void Awake()
        {
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMatchEndedDelegate   = HandleMatchEnded;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onRatingUpdated?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onRatingUpdated?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (!_matchRunning || _ownershipRatingSO == null)
                return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer >= _tickInterval)
            {
                _tickTimer -= _tickInterval;
                int playerZones = _dominanceSO != null ? _dominanceSO.PlayerZoneCount : 0;
                int totalZones  = _ownershipRatingSO.TotalZones;
                _ownershipRatingSO.RecordControlTick(playerZones, totalZones);
            }
        }

        private void HandleMatchStarted()
        {
            _matchRunning = true;
            _tickTimer    = 0f;
            _ownershipRatingSO?.Reset();
            Refresh();
        }

        private void HandleMatchEnded()
        {
            _matchRunning = false;
            Refresh();
        }

        public void Refresh()
        {
            if (_ownershipRatingSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_ratingLabel != null)
                _ratingLabel.text = $"Control: {Mathf.RoundToInt(_ownershipRatingSO.OwnershipRating * 100f)}%";

            if (_ratingBar != null)
                _ratingBar.value = _ownershipRatingSO.OwnershipRating;
        }

        public ZoneControlZoneOwnershipRatingSO OwnershipRatingSO => _ownershipRatingSO;
    }
}

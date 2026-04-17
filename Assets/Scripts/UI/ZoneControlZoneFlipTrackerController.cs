using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that bridges zone-capture events into
    /// <see cref="ZoneControlZoneFlipTrackerSO"/>, awards the milestone bonus
    /// to the player's wallet on each flip milestone, and displays flip progress.
    ///
    /// <c>_onZoneCaptured</c>: records a flip; on milestone completion awards
    ///   wallet bonus + Refresh.
    /// <c>_onMatchStarted</c>: resets the SO + Refresh.
    /// <c>_onFlipMilestoneReached</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlZoneFlipTrackerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneFlipTrackerSO _flipTrackerSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFlipMilestoneReached;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _flipsLabel;
        [SerializeField] private Text       _nextLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private int _milestonesSeenPrev;

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
            _onFlipMilestoneReached?.RegisterCallback(_refreshDelegate);
            _milestonesSeenPrev = _flipTrackerSO?.MilestonesReached ?? 0;
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFlipMilestoneReached?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_flipTrackerSO == null)
            {
                Refresh();
                return;
            }

            int prevMilestones = _flipTrackerSO.MilestonesReached;
            _flipTrackerSO.RecordFlip();

            if (_flipTrackerSO.MilestonesReached > prevMilestones && _wallet != null)
                _wallet.AddFunds(_flipTrackerSO.BonusPerMilestone);

            Refresh();
        }

        private void HandleMatchStarted()
        {
            _flipTrackerSO?.Reset();
            _milestonesSeenPrev = 0;
            Refresh();
        }

        public void Refresh()
        {
            if (_flipTrackerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_flipsLabel != null)
                _flipsLabel.text = $"Flips: {_flipTrackerSO.TotalFlips}";

            if (_nextLabel != null)
                _nextLabel.text = $"Next: {_flipTrackerSO.NextMilestone}";
        }

        public ZoneControlZoneFlipTrackerSO FlipTrackerSO => _flipTrackerSO;
    }
}

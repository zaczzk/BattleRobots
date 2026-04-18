using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMilestoneController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMilestoneSO _milestoneSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMilestoneReached;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _milestoneLabel;
        [SerializeField] private Text       _bonusLabel;
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
            _onMilestoneReached?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMilestoneReached?.UnregisterCallback(_refreshDelegate);
        }

        private void HandleZoneCaptured()
        {
            if (_milestoneSO == null) return;
            int prevMilestones = _milestoneSO.MilestonesReached;
            _milestoneSO.RecordCapture();
            int crossed = _milestoneSO.MilestonesReached - prevMilestones;
            if (crossed > 0)
                _wallet?.AddFunds(crossed * _milestoneSO.BonusPerMilestone);
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _milestoneSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_milestoneSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_milestoneLabel != null)
                _milestoneLabel.text = $"Milestone: {_milestoneSO.MilestonesReached}/{_milestoneSO.MilestoneCount}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Bonus: +{_milestoneSO.TotalBonusAwarded}";
        }

        public ZoneControlCaptureMilestoneSO MilestoneSO => _milestoneSO;
    }
}

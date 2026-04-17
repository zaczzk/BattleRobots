using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlAllZonesDominanceController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlAllZonesDominanceSO _dominanceSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onDominanceStarted;
        [SerializeField] private VoidGameEvent _onDominanceLost;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMilestoneReached;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _elapsedLabel;
        [SerializeField] private Text       _milestoneLabel;
        [SerializeField] private Slider     _progressBar;
        [SerializeField] private GameObject _panel;

        private Action _handleDominanceStartedDelegate;
        private Action _handleDominanceLostDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMilestoneReachedDelegate;

        private void Awake()
        {
            _handleDominanceStartedDelegate  = HandleDominanceStarted;
            _handleDominanceLostDelegate     = HandleDominanceLost;
            _handleMatchStartedDelegate      = HandleMatchStarted;
            _handleMilestoneReachedDelegate  = HandleMilestoneReached;
        }

        private void OnEnable()
        {
            _onDominanceStarted?.RegisterCallback(_handleDominanceStartedDelegate);
            _onDominanceLost?.RegisterCallback(_handleDominanceLostDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMilestoneReached?.RegisterCallback(_handleMilestoneReachedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onDominanceStarted?.UnregisterCallback(_handleDominanceStartedDelegate);
            _onDominanceLost?.UnregisterCallback(_handleDominanceLostDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMilestoneReached?.UnregisterCallback(_handleMilestoneReachedDelegate);
        }

        private void Update()
        {
            _dominanceSO?.Tick(Time.deltaTime);
        }

        private void HandleDominanceStarted()
        {
            _dominanceSO?.StartDominating();
            Refresh();
        }

        private void HandleDominanceLost()
        {
            _dominanceSO?.StopDominating();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _dominanceSO?.Reset();
            Refresh();
        }

        private void HandleMilestoneReached()
        {
            if (_dominanceSO != null)
                _wallet?.AddFunds(_dominanceSO.BonusPerMilestone);
            Refresh();
        }

        public void Refresh()
        {
            if (_dominanceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_elapsedLabel != null)
                _elapsedLabel.text = $"All Zones: {_dominanceSO.AccumulatedTime:F1}s";

            if (_milestoneLabel != null)
                _milestoneLabel.text = $"Milestones: {_dominanceSO.MilestonesReached}";

            if (_progressBar != null)
                _progressBar.value = _dominanceSO.DominanceProgress;
        }

        public ZoneControlAllZonesDominanceSO DominanceSO => _dominanceSO;
    }
}

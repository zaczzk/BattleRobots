using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that drives <see cref="ZoneControlHoldTimeSO"/> hold
    /// tracking, awards wallet bonuses on each milestone, and displays hold
    /// progress and milestone count.
    ///
    /// <c>_onZoneCaptured</c>: starts holding + Refresh.
    /// <c>_onZoneLost</c>: stops holding + Refresh.
    /// <c>_onMatchStarted</c>: resets the SO + Refresh.
    /// <c>_onMilestoneReached</c>: credits wallet + Refresh.
    /// <see cref="Update"/> ticks the SO each frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlHoldTimeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlHoldTimeSO _holdSO;
        [SerializeField] private PlayerWallet          _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onZoneLost;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMilestoneReached;

        [Header("UI References (optional)")]
        [SerializeField] private Slider     _progressBar;
        [SerializeField] private Text       _holdTimeLabel;
        [SerializeField] private Text       _milestoneLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleZoneLostDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMilestoneReachedDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate    = HandleZoneCaptured;
            _handleZoneLostDelegate        = HandleZoneLost;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handleMilestoneReachedDelegate = HandleMilestoneReached;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onZoneLost?.RegisterCallback(_handleZoneLostDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMilestoneReached?.RegisterCallback(_handleMilestoneReachedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onZoneLost?.UnregisterCallback(_handleZoneLostDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMilestoneReached?.UnregisterCallback(_handleMilestoneReachedDelegate);
        }

        private void Update()
        {
            _holdSO?.Tick(Time.deltaTime);
        }

        private void HandleZoneCaptured()
        {
            _holdSO?.StartHolding();
            Refresh();
        }

        private void HandleZoneLost()
        {
            _holdSO?.StopHolding();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _holdSO?.Reset();
            Refresh();
        }

        private void HandleMilestoneReached()
        {
            if (_holdSO != null && _wallet != null)
                _wallet.AddFunds(_holdSO.BonusPerMilestone);
            Refresh();
        }

        public void Refresh()
        {
            if (_holdSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_progressBar != null)
                _progressBar.value = _holdSO.HoldProgress;

            if (_holdTimeLabel != null)
                _holdTimeLabel.text = $"Hold Time: {_holdSO.TotalHoldTime:F1}s";

            if (_milestoneLabel != null)
                _milestoneLabel.text = $"Milestones: {_holdSO.MilestoneCount}";
        }

        public ZoneControlHoldTimeSO HoldSO => _holdSO;
    }
}

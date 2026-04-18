using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlDominanceTimerController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlDominanceTimerSO _dominanceTimerSO;
        [SerializeField] private PlayerWalletSO              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onDominanceAchieved;
        [SerializeField] private VoidGameEvent _onDominanceLost;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDominanceInterval;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _timeLabel;
        [SerializeField] private Text       _milestoneLabel;
        [SerializeField] private Slider     _progressBar;
        [SerializeField] private GameObject _panel;

        private Action _handleDominanceAchievedDelegate;
        private Action _handleDominanceLostDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDominanceIntervalDelegate;

        private void Awake()
        {
            _handleDominanceAchievedDelegate  = HandleDominanceAchieved;
            _handleDominanceLostDelegate      = HandleDominanceLost;
            _handleMatchStartedDelegate       = HandleMatchStarted;
            _handleDominanceIntervalDelegate  = HandleDominanceInterval;
        }

        private void OnEnable()
        {
            _onDominanceAchieved?.RegisterCallback(_handleDominanceAchievedDelegate);
            _onDominanceLost?.RegisterCallback(_handleDominanceLostDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDominanceInterval?.RegisterCallback(_handleDominanceIntervalDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onDominanceAchieved?.UnregisterCallback(_handleDominanceAchievedDelegate);
            _onDominanceLost?.UnregisterCallback(_handleDominanceLostDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDominanceInterval?.UnregisterCallback(_handleDominanceIntervalDelegate);
        }

        private void Update()
        {
            if (_dominanceTimerSO == null || !_dominanceTimerSO.IsDominating) return;
            _dominanceTimerSO.Tick(Time.deltaTime);
            Refresh();
        }

        private void HandleDominanceAchieved()
        {
            _dominanceTimerSO?.StartDominance();
            Refresh();
        }

        private void HandleDominanceLost()
        {
            _dominanceTimerSO?.EndDominance();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _dominanceTimerSO?.Reset();
            Refresh();
        }

        private void HandleDominanceInterval()
        {
            _wallet?.AddFunds(_dominanceTimerSO?.BonusPerInterval ?? 0);
            Refresh();
        }

        public void Refresh()
        {
            if (_dominanceTimerSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_timeLabel != null)
                _timeLabel.text = $"Dominance: {_dominanceTimerSO.TotalDominanceTime:F1}s";

            if (_milestoneLabel != null)
                _milestoneLabel.text = $"Intervals: {_dominanceTimerSO.IntervalsCompleted}";

            if (_progressBar != null)
                _progressBar.value = _dominanceTimerSO.DominanceProgress;
        }

        public ZoneControlDominanceTimerSO DominanceTimerSO => _dominanceTimerSO;
    }
}

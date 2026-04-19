using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchScoreSnapshotController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchScoreSnapshotSO _snapshotSO;
        [SerializeField] private ZoneControlScoreboardSO         _scoreboardSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSnapshotTaken;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _snapshotCountLabel;
        [SerializeField] private Text       _bestScoreLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchEndedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleSnapshotTakenDelegate;

        private void Awake()
        {
            _handleMatchEndedDelegate    = HandleMatchEnded;
            _handleMatchStartedDelegate  = Refresh;
            _handleSnapshotTakenDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSnapshotTaken?.RegisterCallback(_handleSnapshotTakenDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSnapshotTaken?.UnregisterCallback(_handleSnapshotTakenDelegate);
        }

        private void HandleMatchEnded()
        {
            if (_snapshotSO == null) return;
            int score = _scoreboardSO != null ? _scoreboardSO.PlayerScore : 0;
            _snapshotSO.TakeSnapshot(score);
            Refresh();
        }

        public void Refresh()
        {
            if (_snapshotSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_snapshotCountLabel != null)
                _snapshotCountLabel.text = $"Snapshots: {_snapshotSO.SnapshotCount}";

            if (_bestScoreLabel != null)
                _bestScoreLabel.text = $"Best Score: {_snapshotSO.BestScore}";
        }

        public ZoneControlMatchScoreSnapshotSO SnapshotSO => _snapshotSO;
    }
}

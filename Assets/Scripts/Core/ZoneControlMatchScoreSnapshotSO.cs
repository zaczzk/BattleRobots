using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlMatchScoreSnapshot", order = 150)]
    public sealed class ZoneControlMatchScoreSnapshotSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _maxSnapshots = 5;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSnapshotTaken;

        private readonly List<int> _snapshots = new List<int>();

        private void OnEnable() => Reset();

        public int MaxSnapshots   => _maxSnapshots;
        public int SnapshotCount  => _snapshots.Count;
        public int BestScore
        {
            get
            {
                if (_snapshots.Count == 0) return 0;
                int best = _snapshots[0];
                for (int i = 1; i < _snapshots.Count; i++)
                    if (_snapshots[i] > best) best = _snapshots[i];
                return best;
            }
        }

        public void TakeSnapshot(int playerScore)
        {
            if (_snapshots.Count >= _maxSnapshots)
                _snapshots.RemoveAt(0);
            _snapshots.Add(playerScore);
            _onSnapshotTaken?.Raise();
        }

        public int GetSnapshot(int index)
        {
            if (index < 0 || index >= _snapshots.Count) return 0;
            return _snapshots[index];
        }

        public void Reset()
        {
            _snapshots.Clear();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneOwnershipHistory", order = 156)]
    public sealed class ZoneControlZoneOwnershipHistorySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)]          private int   _maxSnapshots      = 5;
        [SerializeField, Range(0f, 1f)]   private float _majorityThreshold = 0.5f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSnapshotAdded;

        private readonly List<float> _ratioHistory = new List<float>();

        private void OnEnable() => Reset();

        public int   SnapshotCount     => _ratioHistory.Count;
        public int   MaxSnapshots      => _maxSnapshots;
        public float MajorityThreshold => _majorityThreshold;

        public float BestRatio
        {
            get
            {
                if (_ratioHistory.Count == 0) return 0f;
                float best = 0f;
                foreach (var r in _ratioHistory)
                    if (r > best) best = r;
                return best;
            }
        }

        public void TakeSnapshot(int playerOwned, int totalZones)
        {
            float ratio = totalZones > 0 ? Mathf.Clamp01(playerOwned / (float)totalZones) : 0f;

            if (_ratioHistory.Count >= _maxSnapshots)
                _ratioHistory.RemoveAt(0);

            _ratioHistory.Add(ratio);
            _onSnapshotAdded?.Raise();
        }

        public int GetMajorityCount()
        {
            int count = 0;
            foreach (var r in _ratioHistory)
                if (r >= _majorityThreshold) count++;
            return count;
        }

        public float GetSnapshot(int index)
        {
            if (index < 0 || index >= _ratioHistory.Count) return 0f;
            return _ratioHistory[index];
        }

        public void Reset() => _ratioHistory.Clear();
    }
}

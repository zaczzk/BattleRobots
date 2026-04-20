using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureNetwork", order = 202)]
    public sealed class ZoneControlCaptureNetworkSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)] private int _nodeCount         = 4;
        [SerializeField, Min(0)] private int _bonusPerNetwork   = 500;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNetworkFired;

        private int _activeNodes;
        private int _networkCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   NodeCount          => _nodeCount;
        public int   BonusPerNetwork    => _bonusPerNetwork;
        public int   ActiveNodes        => _activeNodes;
        public int   NetworkCount       => _networkCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float NodeProgress       => _nodeCount > 0
            ? Mathf.Clamp01(_activeNodes / (float)_nodeCount)
            : 0f;

        public int RecordPlayerCapture()
        {
            if (_activeNodes >= _nodeCount) return 0;
            _activeNodes++;
            if (_activeNodes < _nodeCount) return 0;
            _networkCount++;
            _totalBonusAwarded += _bonusPerNetwork;
            _activeNodes        = 0;
            _onNetworkFired?.Raise();
            return _bonusPerNetwork;
        }

        public void RecordBotCapture()
        {
            if (_activeNodes > 0)
                _activeNodes--;
        }

        public void Reset()
        {
            _activeNodes       = 0;
            _networkCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

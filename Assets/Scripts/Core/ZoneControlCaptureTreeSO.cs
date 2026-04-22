using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTree", order = 351)]
    public sealed class ZoneControlCaptureTreeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _nodesNeeded  = 6;
        [SerializeField, Min(1)] private int _prunePerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerGrow = 2005;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTreeGrown;

        private int _nodes;
        private int _growCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   NodesNeeded       => _nodesNeeded;
        public int   PrunePerBot       => _prunePerBot;
        public int   BonusPerGrow      => _bonusPerGrow;
        public int   Nodes             => _nodes;
        public int   GrowCount         => _growCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float NodeProgress      => _nodesNeeded > 0
            ? Mathf.Clamp01(_nodes / (float)_nodesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _nodes = Mathf.Min(_nodes + 1, _nodesNeeded);
            if (_nodes >= _nodesNeeded)
            {
                int bonus = _bonusPerGrow;
                _growCount++;
                _totalBonusAwarded += bonus;
                _nodes              = 0;
                _onTreeGrown?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _nodes = Mathf.Max(0, _nodes - _prunePerBot);
        }

        public void Reset()
        {
            _nodes             = 0;
            _growCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureNode", order = 354)]
    public sealed class ZoneControlCaptureNodeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _linksNeeded   = 5;
        [SerializeField, Min(1)] private int _cutPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerChain = 2050;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNodeChained;

        private int _links;
        private int _chainCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LinksNeeded       => _linksNeeded;
        public int   CutPerBot         => _cutPerBot;
        public int   BonusPerChain     => _bonusPerChain;
        public int   Links             => _links;
        public int   ChainCount        => _chainCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float LinkProgress      => _linksNeeded > 0
            ? Mathf.Clamp01(_links / (float)_linksNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _links = Mathf.Min(_links + 1, _linksNeeded);
            if (_links >= _linksNeeded)
            {
                int bonus = _bonusPerChain;
                _chainCount++;
                _totalBonusAwarded += bonus;
                _links              = 0;
                _onNodeChained?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _links = Mathf.Max(0, _links - _cutPerBot);
        }

        public void Reset()
        {
            _links             = 0;
            _chainCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

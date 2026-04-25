using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLanglandsCorrespondence", order = 511)]
    public sealed class ZoneControlCaptureLanglandsCorrespondenceSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _matchingPairsNeeded         = 6;
        [SerializeField, Min(1)] private int _lFunctionObstructionsPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerCorrespondence      = 4405;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLanglandsCorrespondenceEstablished;

        private int _matchingPairs;
        private int _correspondenceCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   MatchingPairsNeeded              => _matchingPairsNeeded;
        public int   LFunctionObstructionsPerBot      => _lFunctionObstructionsPerBot;
        public int   BonusPerCorrespondence           => _bonusPerCorrespondence;
        public int   MatchingPairs                    => _matchingPairs;
        public int   CorrespondenceCount              => _correspondenceCount;
        public int   TotalBonusAwarded                => _totalBonusAwarded;
        public float MatchingPairProgress => _matchingPairsNeeded > 0
            ? Mathf.Clamp01(_matchingPairs / (float)_matchingPairsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _matchingPairs = Mathf.Min(_matchingPairs + 1, _matchingPairsNeeded);
            if (_matchingPairs >= _matchingPairsNeeded)
            {
                int bonus = _bonusPerCorrespondence;
                _correspondenceCount++;
                _totalBonusAwarded += bonus;
                _matchingPairs      = 0;
                _onLanglandsCorrespondenceEstablished?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _matchingPairs = Mathf.Max(0, _matchingPairs - _lFunctionObstructionsPerBot);
        }

        public void Reset()
        {
            _matchingPairs      = 0;
            _correspondenceCount = 0;
            _totalBonusAwarded  = 0;
        }
    }
}

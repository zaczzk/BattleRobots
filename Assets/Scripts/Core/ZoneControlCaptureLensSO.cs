using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLens", order = 375)]
    public sealed class ZoneControlCaptureLensSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _facetsNeeded  = 5;
        [SerializeField, Min(1)] private int _scatterPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerView  = 2365;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLensViewed;

        private int _facets;
        private int _viewCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FacetsNeeded       => _facetsNeeded;
        public int   ScatterPerBot      => _scatterPerBot;
        public int   BonusPerView       => _bonusPerView;
        public int   Facets             => _facets;
        public int   ViewCount          => _viewCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float FacetProgress      => _facetsNeeded > 0
            ? Mathf.Clamp01(_facets / (float)_facetsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _facets = Mathf.Min(_facets + 1, _facetsNeeded);
            if (_facets >= _facetsNeeded)
            {
                int bonus = _bonusPerView;
                _viewCount++;
                _totalBonusAwarded += bonus;
                _facets             = 0;
                _onLensViewed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _facets = Mathf.Max(0, _facets - _scatterPerBot);
        }

        public void Reset()
        {
            _facets            = 0;
            _viewCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

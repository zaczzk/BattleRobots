using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHarvest", order = 232)]
    public sealed class ZoneControlCaptureHarvestSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)] private int _capturesPerSeason  = 5;
        [SerializeField, Min(1)] private int _seasonsForHarvest  = 3;
        [SerializeField, Min(0)] private int _bonusPerHarvest    = 400;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHarvest;

        private int _seasonCaptures;
        private int _seasonCount;
        private int _harvestCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CapturesPerSeason  => _capturesPerSeason;
        public int   SeasonsForHarvest  => _seasonsForHarvest;
        public int   BonusPerHarvest    => _bonusPerHarvest;
        public int   SeasonCaptures     => _seasonCaptures;
        public int   SeasonCount        => _seasonCount;
        public int   HarvestCount       => _harvestCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float SeasonProgress     => _capturesPerSeason > 0
            ? Mathf.Clamp01(_seasonCaptures / (float)_capturesPerSeason)
            : 0f;

        public int RecordPlayerCapture()
        {
            _seasonCaptures++;
            if (_seasonCaptures >= _capturesPerSeason)
            {
                _seasonCaptures = 0;
                _seasonCount++;
                if (_seasonCount >= _seasonsForHarvest)
                {
                    int bonus = _bonusPerHarvest;
                    _harvestCount++;
                    _totalBonusAwarded += bonus;
                    _seasonCount        = 0;
                    _onHarvest?.Raise();
                    return bonus;
                }
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _seasonCaptures = Mathf.Max(0, _seasonCaptures - 1);
        }

        public void Reset()
        {
            _seasonCaptures    = 0;
            _seasonCount       = 0;
            _harvestCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

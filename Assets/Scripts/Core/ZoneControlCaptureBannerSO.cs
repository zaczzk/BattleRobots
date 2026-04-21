using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBanner", order = 261)]
    public sealed class ZoneControlCaptureBannerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _emblemsNeeded  = 5;
        [SerializeField, Min(1)] private int _tearPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerBanner = 655;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onBannerRaised;

        private int _emblems;
        private int _bannerCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   EmblemsNeeded     => _emblemsNeeded;
        public int   TearPerBot        => _tearPerBot;
        public int   BonusPerBanner    => _bonusPerBanner;
        public int   Emblems           => _emblems;
        public int   BannerCount       => _bannerCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float EmblemProgress    => _emblemsNeeded > 0
            ? Mathf.Clamp01(_emblems / (float)_emblemsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _emblems = Mathf.Min(_emblems + 1, _emblemsNeeded);
            if (_emblems >= _emblemsNeeded)
            {
                int bonus = _bonusPerBanner;
                _bannerCount++;
                _totalBonusAwarded += bonus;
                _emblems            = 0;
                _onBannerRaised?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _emblems = Mathf.Max(0, _emblems - _tearPerBot);
        }

        public void Reset()
        {
            _emblems           = 0;
            _bannerCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

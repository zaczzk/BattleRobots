using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSite", order = 449)]
    public sealed class ZoneControlCaptureSiteSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _coveringsNeeded  = 6;
        [SerializeField, Min(1)] private int _sievePerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerCovering = 3460;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSiteCovered;

        private int _coverings;
        private int _coveringCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CoveringsNeeded   => _coveringsNeeded;
        public int   SievePerBot       => _sievePerBot;
        public int   BonusPerCovering  => _bonusPerCovering;
        public int   Coverings         => _coverings;
        public int   CoveringCount     => _coveringCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float SiteProgress      => _coveringsNeeded > 0
            ? Mathf.Clamp01(_coverings / (float)_coveringsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _coverings = Mathf.Min(_coverings + 1, _coveringsNeeded);
            if (_coverings >= _coveringsNeeded)
            {
                int bonus = _bonusPerCovering;
                _coveringCount++;
                _totalBonusAwarded += bonus;
                _coverings          = 0;
                _onSiteCovered?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _coverings = Mathf.Max(0, _coverings - _sievePerBot);
        }

        public void Reset()
        {
            _coverings         = 0;
            _coveringCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePennant", order = 269)]
    public sealed class ZoneControlCapturePennantSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _stripesNeeded   = 5;
        [SerializeField, Min(1)] private int _tearPerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerPennant = 775;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPennantRaised;

        private int _stripes;
        private int _pennantCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StripesNeeded     => _stripesNeeded;
        public int   TearPerBot        => _tearPerBot;
        public int   BonusPerPennant   => _bonusPerPennant;
        public int   Stripes           => _stripes;
        public int   PennantCount      => _pennantCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float StripeProgress    => _stripesNeeded > 0
            ? Mathf.Clamp01(_stripes / (float)_stripesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _stripes = Mathf.Min(_stripes + 1, _stripesNeeded);
            if (_stripes >= _stripesNeeded)
            {
                int bonus = _bonusPerPennant;
                _pennantCount++;
                _totalBonusAwarded += bonus;
                _stripes            = 0;
                _onPennantRaised?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _stripes = Mathf.Max(0, _stripes - _tearPerBot);
        }

        public void Reset()
        {
            _stripes           = 0;
            _pennantCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

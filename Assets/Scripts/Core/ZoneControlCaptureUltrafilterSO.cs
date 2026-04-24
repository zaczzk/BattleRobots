using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureUltrafilter", order = 445)]
    public sealed class ZoneControlCaptureUltrafilterSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _ultrasetsNeeded   = 5;
        [SerializeField, Min(1)] private int _dilutePerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerUltrafine = 3415;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onUltrafilterRefined;

        private int _ultrasets;
        private int _ultrafineCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   UltrasetsNeeded    => _ultrasetsNeeded;
        public int   DilutePerBot       => _dilutePerBot;
        public int   BonusPerUltrafine  => _bonusPerUltrafine;
        public int   Ultrasets          => _ultrasets;
        public int   UltrafineCount     => _ultrafineCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float UltrafilterProgress => _ultrasetsNeeded > 0
            ? Mathf.Clamp01(_ultrasets / (float)_ultrasetsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _ultrasets = Mathf.Min(_ultrasets + 1, _ultrasetsNeeded);
            if (_ultrasets >= _ultrasetsNeeded)
            {
                int bonus = _bonusPerUltrafine;
                _ultrafineCount++;
                _totalBonusAwarded += bonus;
                _ultrasets          = 0;
                _onUltrafilterRefined?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _ultrasets = Mathf.Max(0, _ultrasets - _dilutePerBot);
        }

        public void Reset()
        {
            _ultrasets         = 0;
            _ultrafineCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}

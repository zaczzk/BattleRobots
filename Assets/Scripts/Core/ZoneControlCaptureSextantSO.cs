using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSextant", order = 281)]
    public sealed class ZoneControlCaptureSextantSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _sightingsNeeded = 5;
        [SerializeField, Min(1)] private int _obscurePerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerFix     = 955;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSextantFixed;

        private int _sightings;
        private int _fixCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SightingsNeeded    => _sightingsNeeded;
        public int   ObscurePerBot      => _obscurePerBot;
        public int   BonusPerFix        => _bonusPerFix;
        public int   Sightings          => _sightings;
        public int   FixCount           => _fixCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float SightingProgress   => _sightingsNeeded > 0
            ? Mathf.Clamp01(_sightings / (float)_sightingsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _sightings = Mathf.Min(_sightings + 1, _sightingsNeeded);
            if (_sightings >= _sightingsNeeded)
            {
                int bonus = _bonusPerFix;
                _fixCount++;
                _totalBonusAwarded += bonus;
                _sightings          = 0;
                _onSextantFixed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _sightings = Mathf.Max(0, _sightings - _obscurePerBot);
        }

        public void Reset()
        {
            _sightings         = 0;
            _fixCount          = 0;
            _totalBonusAwarded = 0;
        }
    }
}

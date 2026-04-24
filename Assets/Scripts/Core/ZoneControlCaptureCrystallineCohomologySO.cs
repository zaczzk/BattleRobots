using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCrystallineCohomology", order = 473)]
    public sealed class ZoneControlCaptureCrystallineCohomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _liftsNeeded              = 5;
        [SerializeField, Min(1)] private int _breakPerBot              = 1;
        [SerializeField, Min(0)] private int _bonusPerCrystallization  = 3835;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCrystallineCohomologyCrystallized;

        private int _lifts;
        private int _crystallizeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LiftsNeeded             => _liftsNeeded;
        public int   BreakPerBot             => _breakPerBot;
        public int   BonusPerCrystallization => _bonusPerCrystallization;
        public int   Lifts                   => _lifts;
        public int   CrystallizeCount        => _crystallizeCount;
        public int   TotalBonusAwarded       => _totalBonusAwarded;
        public float LiftProgress            => _liftsNeeded > 0
            ? Mathf.Clamp01(_lifts / (float)_liftsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _lifts = Mathf.Min(_lifts + 1, _liftsNeeded);
            if (_lifts >= _liftsNeeded)
            {
                int bonus = _bonusPerCrystallization;
                _crystallizeCount++;
                _totalBonusAwarded += bonus;
                _lifts              = 0;
                _onCrystallineCohomologyCrystallized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _lifts = Mathf.Max(0, _lifts - _breakPerBot);
        }

        public void Reset()
        {
            _lifts             = 0;
            _crystallizeCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

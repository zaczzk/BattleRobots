using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMonoid", order = 370)]
    public sealed class ZoneControlCaptureMonoidSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _unitsNeeded      = 7;
        [SerializeField, Min(1)] private int _resetPerBot      = 2;
        [SerializeField, Min(0)] private int _bonusPerIdentity = 2290;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMonoidIdentified;

        private int _units;
        private int _identityCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   UnitsNeeded        => _unitsNeeded;
        public int   ResetPerBot        => _resetPerBot;
        public int   BonusPerIdentity   => _bonusPerIdentity;
        public int   Units              => _units;
        public int   IdentityCount      => _identityCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float UnitProgress       => _unitsNeeded > 0
            ? Mathf.Clamp01(_units / (float)_unitsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _units = Mathf.Min(_units + 1, _unitsNeeded);
            if (_units >= _unitsNeeded)
            {
                int bonus = _bonusPerIdentity;
                _identityCount++;
                _totalBonusAwarded += bonus;
                _units              = 0;
                _onMonoidIdentified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _units = Mathf.Max(0, _units - _resetPerBot);
        }

        public void Reset()
        {
            _units             = 0;
            _identityCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

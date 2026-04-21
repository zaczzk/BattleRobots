using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureArmature", order = 307)]
    public sealed class ZoneControlCaptureArmatureSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _coilsNeeded   = 5;
        [SerializeField, Min(1)] private int _shortPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerWinding = 1345;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onArmatureWound;

        private int _coils;
        private int _windingCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CoilsNeeded      => _coilsNeeded;
        public int   ShortPerBot      => _shortPerBot;
        public int   BonusPerWinding  => _bonusPerWinding;
        public int   Coils            => _coils;
        public int   WindingCount     => _windingCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float CoilProgress     => _coilsNeeded > 0
            ? Mathf.Clamp01(_coils / (float)_coilsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _coils = Mathf.Min(_coils + 1, _coilsNeeded);
            if (_coils >= _coilsNeeded)
            {
                int bonus = _bonusPerWinding;
                _windingCount++;
                _totalBonusAwarded += bonus;
                _coils              = 0;
                _onArmatureWound?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _coils = Mathf.Max(0, _coils - _shortPerBot);
        }

        public void Reset()
        {
            _coils             = 0;
            _windingCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

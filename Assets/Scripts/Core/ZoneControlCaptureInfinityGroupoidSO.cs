using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureInfinityGroupoid", order = 495)]
    public sealed class ZoneControlCaptureInfinityGroupoidSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _hornFillingsNeeded       = 5;
        [SerializeField, Min(1)] private int _degenerateSimplicesPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerFill              = 4165;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onInfinityGroupoidFilled;

        private int _hornFillings;
        private int _fillCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   HornFillingsNeeded       => _hornFillingsNeeded;
        public int   DegenerateSimplicesPerBot => _degenerateSimplicesPerBot;
        public int   BonusPerFill              => _bonusPerFill;
        public int   HornFillings              => _hornFillings;
        public int   FillCount                 => _fillCount;
        public int   TotalBonusAwarded         => _totalBonusAwarded;
        public float HornFillingProgress => _hornFillingsNeeded > 0
            ? Mathf.Clamp01(_hornFillings / (float)_hornFillingsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _hornFillings = Mathf.Min(_hornFillings + 1, _hornFillingsNeeded);
            if (_hornFillings >= _hornFillingsNeeded)
            {
                int bonus = _bonusPerFill;
                _fillCount++;
                _totalBonusAwarded += bonus;
                _hornFillings       = 0;
                _onInfinityGroupoidFilled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _hornFillings = Mathf.Max(0, _hornFillings - _degenerateSimplicesPerBot);
        }

        public void Reset()
        {
            _hornFillings      = 0;
            _fillCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

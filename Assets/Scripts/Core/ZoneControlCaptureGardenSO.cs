using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGarden", order = 253)]
    public sealed class ZoneControlCaptureGardenSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _bloomsNeeded   = 5;
        [SerializeField, Min(1)] private int _wiltPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerBloom  = 535;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGardenBloomed;

        private int _blooms;
        private int _gardenCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BloomsNeeded      => _bloomsNeeded;
        public int   WiltPerBot        => _wiltPerBot;
        public int   BonusPerBloom     => _bonusPerBloom;
        public int   Blooms            => _blooms;
        public int   GardenCount       => _gardenCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float BloomProgress     => _bloomsNeeded > 0
            ? Mathf.Clamp01(_blooms / (float)_bloomsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _blooms = Mathf.Min(_blooms + 1, _bloomsNeeded);
            if (_blooms >= _bloomsNeeded)
            {
                int bonus = _bonusPerBloom;
                _gardenCount++;
                _totalBonusAwarded += bonus;
                _blooms             = 0;
                _onGardenBloomed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _blooms = Mathf.Max(0, _blooms - _wiltPerBot);
        }

        public void Reset()
        {
            _blooms            = 0;
            _gardenCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

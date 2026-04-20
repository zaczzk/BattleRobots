using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureTotem", order = 242)]
    public sealed class ZoneControlCaptureTotemSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _ringsNeeded    = 5;
        [SerializeField, Min(1)] private int _ringsPerTopple = 2;
        [SerializeField, Min(0)] private int _bonusPerTotem  = 490;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTotemRaised;

        private int _rings;
        private int _totemCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RingsNeeded       => _ringsNeeded;
        public int   RingsPerTopple    => _ringsPerTopple;
        public int   BonusPerTotem     => _bonusPerTotem;
        public int   Rings             => _rings;
        public int   TotemCount        => _totemCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float RingProgress      => _ringsNeeded > 0
            ? Mathf.Clamp01(_rings / (float)_ringsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _rings = Mathf.Min(_rings + 1, _ringsNeeded);
            if (_rings >= _ringsNeeded)
            {
                int bonus = _bonusPerTotem;
                _totemCount++;
                _totalBonusAwarded += bonus;
                _rings              = 0;
                _onTotemRaised?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _rings = Mathf.Max(0, _rings - _ringsPerTopple);
        }

        public void Reset()
        {
            _rings             = 0;
            _totemCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

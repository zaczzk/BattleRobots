using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSearch", order = 357)]
    public sealed class ZoneControlCaptureSearchSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _probesNeeded     = 7;
        [SerializeField, Min(1)] private int _missPerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerFind     = 2095;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onTargetFound;

        private int _probes;
        private int _findCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ProbesNeeded      => _probesNeeded;
        public int   MissPerBot        => _missPerBot;
        public int   BonusPerFind      => _bonusPerFind;
        public int   Probes            => _probes;
        public int   FindCount         => _findCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float ProbeProgress     => _probesNeeded > 0
            ? Mathf.Clamp01(_probes / (float)_probesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _probes = Mathf.Min(_probes + 1, _probesNeeded);
            if (_probes >= _probesNeeded)
            {
                int bonus = _bonusPerFind;
                _findCount++;
                _totalBonusAwarded += bonus;
                _probes             = 0;
                _onTargetFound?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _probes = Mathf.Max(0, _probes - _missPerBot);
        }

        public void Reset()
        {
            _probes            = 0;
            _findCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

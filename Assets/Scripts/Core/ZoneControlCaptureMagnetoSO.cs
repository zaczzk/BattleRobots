using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMagneto", order = 314)]
    public sealed class ZoneControlCaptureMagnetoSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _polesNeeded   = 5;
        [SerializeField, Min(1)] private int _dampPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerPulse = 1450;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMagnetoCharged;

        private int _poles;
        private int _pulseCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PolesNeeded      => _polesNeeded;
        public int   DampPerBot       => _dampPerBot;
        public int   BonusPerPulse    => _bonusPerPulse;
        public int   Poles            => _poles;
        public int   PulseCount       => _pulseCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float PoleProgress     => _polesNeeded > 0
            ? Mathf.Clamp01(_poles / (float)_polesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _poles = Mathf.Min(_poles + 1, _polesNeeded);
            if (_poles >= _polesNeeded)
            {
                int bonus = _bonusPerPulse;
                _pulseCount++;
                _totalBonusAwarded += bonus;
                _poles              = 0;
                _onMagnetoCharged?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _poles = Mathf.Max(0, _poles - _dampPerBot);
        }

        public void Reset()
        {
            _poles             = 0;
            _pulseCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

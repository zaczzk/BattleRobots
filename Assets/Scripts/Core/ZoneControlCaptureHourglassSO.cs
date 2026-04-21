using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHourglass", order = 271)]
    public sealed class ZoneControlCaptureHourglassSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _grainsNeeded      = 5;
        [SerializeField, Min(1)] private int _spillPerBot        = 1;
        [SerializeField, Min(0)] private int _bonusPerInversion  = 805;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHourglassInverted;

        private int _grains;
        private int _inversionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   GrainsNeeded       => _grainsNeeded;
        public int   SpillPerBot        => _spillPerBot;
        public int   BonusPerInversion  => _bonusPerInversion;
        public int   Grains             => _grains;
        public int   InversionCount     => _inversionCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float GrainProgress      => _grainsNeeded > 0
            ? Mathf.Clamp01(_grains / (float)_grainsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _grains = Mathf.Min(_grains + 1, _grainsNeeded);
            if (_grains >= _grainsNeeded)
            {
                int bonus = _bonusPerInversion;
                _inversionCount++;
                _totalBonusAwarded += bonus;
                _grains             = 0;
                _onHourglassInverted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _grains = Mathf.Max(0, _grains - _spillPerBot);
        }

        public void Reset()
        {
            _grains            = 0;
            _inversionCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}

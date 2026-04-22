using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRectifier", order = 316)]
    public sealed class ZoneControlCaptureRectifierSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _wavesNeeded        = 6;
        [SerializeField, Min(1)] private int _ripplePerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerConversion = 1480;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRectifierConverted;

        private int _waves;
        private int _conversionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   WavesNeeded        => _wavesNeeded;
        public int   RipplePerBot       => _ripplePerBot;
        public int   BonusPerConversion => _bonusPerConversion;
        public int   Waves              => _waves;
        public int   ConversionCount    => _conversionCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float WaveProgress       => _wavesNeeded > 0
            ? Mathf.Clamp01(_waves / (float)_wavesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _waves = Mathf.Min(_waves + 1, _wavesNeeded);
            if (_waves >= _wavesNeeded)
            {
                int bonus = _bonusPerConversion;
                _conversionCount++;
                _totalBonusAwarded += bonus;
                _waves              = 0;
                _onRectifierConverted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _waves = Mathf.Max(0, _waves - _ripplePerBot);
        }

        public void Reset()
        {
            _waves             = 0;
            _conversionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

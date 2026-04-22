using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDemodulator", order = 327)]
    public sealed class ZoneControlCaptureDemodulatorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _samplesNeeded       = 6;
        [SerializeField, Min(1)] private int _noisePerBot         = 2;
        [SerializeField, Min(0)] private int _bonusPerExtraction  = 1645;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDemodulatorExtracted;

        private int _samples;
        private int _extractionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SamplesNeeded      => _samplesNeeded;
        public int   NoisePerBot        => _noisePerBot;
        public int   BonusPerExtraction => _bonusPerExtraction;
        public int   Samples            => _samples;
        public int   ExtractionCount    => _extractionCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float SampleProgress     => _samplesNeeded > 0
            ? Mathf.Clamp01(_samples / (float)_samplesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _samples = Mathf.Min(_samples + 1, _samplesNeeded);
            if (_samples >= _samplesNeeded)
            {
                int bonus = _bonusPerExtraction;
                _extractionCount++;
                _totalBonusAwarded += bonus;
                _samples            = 0;
                _onDemodulatorExtracted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _samples = Mathf.Max(0, _samples - _noisePerBot);
        }

        public void Reset()
        {
            _samples           = 0;
            _extractionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

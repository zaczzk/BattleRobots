using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMeasure", order = 440)]
    public sealed class ZoneControlCaptureMeasureSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _samplesNeeded      = 6;
        [SerializeField, Min(1)] private int _removePerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerIntegration = 3340;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMeasureIntegrated;

        private int _samples;
        private int _integrationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SamplesNeeded      => _samplesNeeded;
        public int   RemovePerBot       => _removePerBot;
        public int   BonusPerIntegration => _bonusPerIntegration;
        public int   Samples            => _samples;
        public int   IntegrationCount   => _integrationCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float SampleProgress     => _samplesNeeded > 0
            ? Mathf.Clamp01(_samples / (float)_samplesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _samples = Mathf.Min(_samples + 1, _samplesNeeded);
            if (_samples >= _samplesNeeded)
            {
                int bonus = _bonusPerIntegration;
                _integrationCount++;
                _totalBonusAwarded += bonus;
                _samples            = 0;
                _onMeasureIntegrated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _samples = Mathf.Max(0, _samples - _removePerBot);
        }

        public void Reset()
        {
            _samples           = 0;
            _integrationCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

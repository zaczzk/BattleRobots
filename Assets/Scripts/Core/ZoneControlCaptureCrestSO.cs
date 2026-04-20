using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCrest", order = 208)]
    public sealed class ZoneControlCaptureCrestSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.1f)] private float _waveRisePerCapture    = 1f;
        [SerializeField, Min(0f)]   private float _waveFallPerBotCapture = 0.5f;
        [SerializeField, Min(1f)]   private float _waveHeightForCrest    = 5f;
        [SerializeField, Min(0)]    private int   _bonusPerCrest         = 400;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCrest;

        private float _currentWaveHeight;
        private int   _crestCount;
        private int   _totalBonusAwarded;

        private void OnEnable() => Reset();

        public float WaveRisePerCapture    => _waveRisePerCapture;
        public float WaveFallPerBotCapture => _waveFallPerBotCapture;
        public float WaveHeightForCrest    => _waveHeightForCrest;
        public int   BonusPerCrest         => _bonusPerCrest;
        public float CurrentWaveHeight     => _currentWaveHeight;
        public int   CrestCount            => _crestCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float WaveProgress          => _waveHeightForCrest > 0f
            ? Mathf.Clamp01(_currentWaveHeight / _waveHeightForCrest)
            : 0f;

        public void RecordPlayerCapture()
        {
            _currentWaveHeight = Mathf.Min(_currentWaveHeight + _waveRisePerCapture, _waveHeightForCrest);
            if (_currentWaveHeight >= _waveHeightForCrest)
                Crest();
        }

        private void Crest()
        {
            _crestCount++;
            _totalBonusAwarded += _bonusPerCrest;
            _currentWaveHeight  = 0f;
            _onCrest?.Raise();
        }

        public void RecordBotCapture()
        {
            _currentWaveHeight = Mathf.Max(0f, _currentWaveHeight - _waveFallPerBotCapture);
        }

        public void Reset()
        {
            _currentWaveHeight = 0f;
            _crestCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

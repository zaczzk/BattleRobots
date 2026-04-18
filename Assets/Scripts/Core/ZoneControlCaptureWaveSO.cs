using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureWave", order = 138)]
    public sealed class ZoneControlCaptureWaveSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0.5f)] private float _waveCooldown     = 8f;
        [SerializeField, Min(0)]    private int   _pointsPerCapture = 25;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onWaveScored;

        private int   _currentWaveCaptures;
        private int   _totalWavesScored;
        private int   _bestWaveCaptures;
        private int   _totalBonusAwarded;
        private int   _lastWaveBonus;
        private float _waveTimer;
        private bool  _waveActive;

        private void OnEnable() => Reset();

        public float WaveCooldown         => _waveCooldown;
        public int   PointsPerCapture     => _pointsPerCapture;
        public int   CurrentWaveCaptures  => _currentWaveCaptures;
        public int   TotalWavesScored     => _totalWavesScored;
        public int   BestWaveCaptures     => _bestWaveCaptures;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public int   LastWaveBonus        => _lastWaveBonus;
        public bool  IsWaveActive         => _waveActive;

        public void RecordCapture()
        {
            _currentWaveCaptures++;
            _waveActive = true;
            _waveTimer  = 0f;
        }

        public void Tick(float dt)
        {
            if (!_waveActive) return;
            _waveTimer += dt;
            if (_waveTimer >= _waveCooldown)
                ScoreWave();
        }

        public void Reset()
        {
            _currentWaveCaptures = 0;
            _totalWavesScored    = 0;
            _bestWaveCaptures    = 0;
            _totalBonusAwarded   = 0;
            _lastWaveBonus       = 0;
            _waveTimer           = 0f;
            _waveActive          = false;
        }

        private void ScoreWave()
        {
            if (_currentWaveCaptures <= 0)
            {
                _waveActive = false;
                _waveTimer  = 0f;
                return;
            }

            int bonus = _currentWaveCaptures * _pointsPerCapture;
            if (_currentWaveCaptures > _bestWaveCaptures)
                _bestWaveCaptures = _currentWaveCaptures;

            _totalWavesScored++;
            _totalBonusAwarded   += bonus;
            _lastWaveBonus        = bonus;
            _currentWaveCaptures  = 0;
            _waveTimer            = 0f;
            _waveActive           = false;
            _onWaveScored?.Raise();
        }
    }
}

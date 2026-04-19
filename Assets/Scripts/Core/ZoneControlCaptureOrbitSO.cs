using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureOrbit", order = 185)]
    public sealed class ZoneControlCaptureOrbitSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _orbitTarget         = 5;
        [SerializeField, Min(1)] private int _drainPerBotCapture  = 1;
        [SerializeField, Min(0)] private int _bonusPerOrbit       = 250;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onOrbit;

        private int _currentOrbit;
        private int _orbitCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   OrbitTarget        => _orbitTarget;
        public int   DrainPerBotCapture => _drainPerBotCapture;
        public int   BonusPerOrbit      => _bonusPerOrbit;
        public int   CurrentOrbit       => _currentOrbit;
        public int   OrbitCount         => _orbitCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float OrbitProgress      => _orbitTarget > 0 ? Mathf.Clamp01((float)_currentOrbit / _orbitTarget) : 0f;

        public void RecordPlayerCapture()
        {
            _currentOrbit++;
            if (_currentOrbit >= _orbitTarget)
                TriggerOrbit();
        }

        public void RecordBotCapture()
        {
            _currentOrbit = Mathf.Max(0, _currentOrbit - _drainPerBotCapture);
        }

        private void TriggerOrbit()
        {
            _orbitCount++;
            _totalBonusAwarded += _bonusPerOrbit;
            _currentOrbit       = 0;
            _onOrbit?.Raise();
        }

        public void Reset()
        {
            _currentOrbit      = 0;
            _orbitCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

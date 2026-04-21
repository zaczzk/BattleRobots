using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureVane", order = 278)]
    public sealed class ZoneControlCaptureVaneSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _spinsNeeded      = 4;
        [SerializeField, Min(1)] private int _brakePerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerRotation = 910;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onVaneSpun;

        private int _spins;
        private int _rotationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   SpinsNeeded       => _spinsNeeded;
        public int   BrakePerBot       => _brakePerBot;
        public int   BonusPerRotation  => _bonusPerRotation;
        public int   Spins             => _spins;
        public int   RotationCount     => _rotationCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float SpinProgress      => _spinsNeeded > 0
            ? Mathf.Clamp01(_spins / (float)_spinsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _spins = Mathf.Min(_spins + 1, _spinsNeeded);
            if (_spins >= _spinsNeeded)
            {
                int bonus = _bonusPerRotation;
                _rotationCount++;
                _totalBonusAwarded += bonus;
                _spins              = 0;
                _onVaneSpun?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _spins = Mathf.Max(0, _spins - _brakePerBot);
        }

        public void Reset()
        {
            _spins             = 0;
            _rotationCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

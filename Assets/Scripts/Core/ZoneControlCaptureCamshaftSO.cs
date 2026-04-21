using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCamshaft", order = 299)]
    public sealed class ZoneControlCaptureCamshaftSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _lobesNeeded       = 5;
        [SerializeField, Min(1)] private int _wearPerBot        = 1;
        [SerializeField, Min(0)] private int _bonusPerRotation  = 1225;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCamshaftRotated;

        private int _lobes;
        private int _rotationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LobesNeeded       => _lobesNeeded;
        public int   WearPerBot        => _wearPerBot;
        public int   BonusPerRotation  => _bonusPerRotation;
        public int   Lobes             => _lobes;
        public int   RotationCount     => _rotationCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float LobeProgress      => _lobesNeeded > 0
            ? Mathf.Clamp01(_lobes / (float)_lobesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _lobes = Mathf.Min(_lobes + 1, _lobesNeeded);
            if (_lobes >= _lobesNeeded)
            {
                int bonus = _bonusPerRotation;
                _rotationCount++;
                _totalBonusAwarded += bonus;
                _lobes              = 0;
                _onCamshaftRotated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _lobes = Mathf.Max(0, _lobes - _wearPerBot);
        }

        public void Reset()
        {
            _lobes             = 0;
            _rotationCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

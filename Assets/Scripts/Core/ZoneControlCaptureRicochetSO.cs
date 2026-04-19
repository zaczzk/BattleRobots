using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRicochet", order = 176)]
    public sealed class ZoneControlCaptureRicochetSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _ricochetBonus = 200;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRicochet;

        private bool _isArmed;
        private int  _ricochetCount;
        private int  _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int  RicochetBonus     => _ricochetBonus;
        public bool IsArmed           => _isArmed;
        public int  RicochetCount     => _ricochetCount;
        public int  TotalBonusAwarded => _totalBonusAwarded;

        public void RecordBotCapture()
        {
            _isArmed = true;
        }

        public void RecordPlayerCapture()
        {
            if (!_isArmed) return;
            _isArmed = false;
            _ricochetCount++;
            _totalBonusAwarded += _ricochetBonus;
            _onRicochet?.Raise();
        }

        public void Reset()
        {
            _isArmed           = false;
            _ricochetCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

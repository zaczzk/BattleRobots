using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureWinch", order = 292)]
    public sealed class ZoneControlCaptureWinchSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _cranksNeeded  = 6;
        [SerializeField, Min(1)] private int _slackPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerHaul  = 1120;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onWinchHauled;

        private int _cranks;
        private int _haulCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CranksNeeded      => _cranksNeeded;
        public int   SlackPerBot       => _slackPerBot;
        public int   BonusPerHaul      => _bonusPerHaul;
        public int   Cranks            => _cranks;
        public int   HaulCount         => _haulCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float CrankProgress     => _cranksNeeded > 0
            ? Mathf.Clamp01(_cranks / (float)_cranksNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _cranks = Mathf.Min(_cranks + 1, _cranksNeeded);
            if (_cranks >= _cranksNeeded)
            {
                int bonus = _bonusPerHaul;
                _haulCount++;
                _totalBonusAwarded += bonus;
                _cranks             = 0;
                _onWinchHauled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _cranks = Mathf.Max(0, _cranks - _slackPerBot);
        }

        public void Reset()
        {
            _cranks            = 0;
            _haulCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

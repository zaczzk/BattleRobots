using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLattice", order = 432)]
    public sealed class ZoneControlCaptureLatticeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _joinsNeeded      = 5;
        [SerializeField, Min(1)] private int _collapsePerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerJoin     = 3220;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onJoinFormed;

        private int _joins;
        private int _joinCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   JoinsNeeded       => _joinsNeeded;
        public int   CollapsePerBot    => _collapsePerBot;
        public int   BonusPerJoin      => _bonusPerJoin;
        public int   Joins             => _joins;
        public int   JoinCount         => _joinCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float JoinProgress      => _joinsNeeded > 0
            ? Mathf.Clamp01(_joins / (float)_joinsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _joins = Mathf.Min(_joins + 1, _joinsNeeded);
            if (_joins >= _joinsNeeded)
            {
                int bonus = _bonusPerJoin;
                _joinCount++;
                _totalBonusAwarded += bonus;
                _joins              = 0;
                _onJoinFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _joins = Mathf.Max(0, _joins - _collapsePerBot);
        }

        public void Reset()
        {
            _joins             = 0;
            _joinCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

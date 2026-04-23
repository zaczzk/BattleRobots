using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSemilattice", order = 431)]
    public sealed class ZoneControlCaptureSemilatticeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _meetsNeeded      = 6;
        [SerializeField, Min(1)] private int _dissolvePerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerMeet     = 3205;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMeetFormed;

        private int _meets;
        private int _meetCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   MeetsNeeded       => _meetsNeeded;
        public int   DissolvePerBot    => _dissolvePerBot;
        public int   BonusPerMeet      => _bonusPerMeet;
        public int   Meets             => _meets;
        public int   MeetCount         => _meetCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float MeetProgress      => _meetsNeeded > 0
            ? Mathf.Clamp01(_meets / (float)_meetsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _meets = Mathf.Min(_meets + 1, _meetsNeeded);
            if (_meets >= _meetsNeeded)
            {
                int bonus = _bonusPerMeet;
                _meetCount++;
                _totalBonusAwarded += bonus;
                _meets              = 0;
                _onMeetFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _meets = Mathf.Max(0, _meets - _dissolvePerBot);
        }

        public void Reset()
        {
            _meets             = 0;
            _meetCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

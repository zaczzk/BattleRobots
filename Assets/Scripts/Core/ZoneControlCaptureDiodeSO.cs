using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDiode", order = 323)]
    public sealed class ZoneControlCaptureDiodeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _junctionsNeeded    = 6;
        [SerializeField, Min(1)] private int _reversePerBot      = 2;
        [SerializeField, Min(0)] private int _bonusPerConduction = 1585;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDiodeConducted;

        private int _junctions;
        private int _conductionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   JunctionsNeeded    => _junctionsNeeded;
        public int   ReversePerBot      => _reversePerBot;
        public int   BonusPerConduction => _bonusPerConduction;
        public int   Junctions          => _junctions;
        public int   ConductionCount    => _conductionCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float JunctionProgress   => _junctionsNeeded > 0
            ? Mathf.Clamp01(_junctions / (float)_junctionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _junctions = Mathf.Min(_junctions + 1, _junctionsNeeded);
            if (_junctions >= _junctionsNeeded)
            {
                int bonus = _bonusPerConduction;
                _conductionCount++;
                _totalBonusAwarded += bonus;
                _junctions          = 0;
                _onDiodeConducted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _junctions = Mathf.Max(0, _junctions - _reversePerBot);
        }

        public void Reset()
        {
            _junctions         = 0;
            _conductionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureColimit", order = 402)]
    public sealed class ZoneControlCaptureColimitSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _diagramsNeeded  = 7;
        [SerializeField, Min(1)] private int _dissolvePerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerColimit = 2770;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onColimitComputed;

        private int _diagrams;
        private int _colimitCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   DiagramsNeeded    => _diagramsNeeded;
        public int   DissolvePerBot    => _dissolvePerBot;
        public int   BonusPerColimit   => _bonusPerColimit;
        public int   Diagrams          => _diagrams;
        public int   ColimitCount      => _colimitCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float DiagramProgress   => _diagramsNeeded > 0
            ? Mathf.Clamp01(_diagrams / (float)_diagramsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _diagrams = Mathf.Min(_diagrams + 1, _diagramsNeeded);
            if (_diagrams >= _diagramsNeeded)
            {
                int bonus = _bonusPerColimit;
                _colimitCount++;
                _totalBonusAwarded += bonus;
                _diagrams           = 0;
                _onColimitComputed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _diagrams = Mathf.Max(0, _diagrams - _dissolvePerBot);
        }

        public void Reset()
        {
            _diagrams          = 0;
            _colimitCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

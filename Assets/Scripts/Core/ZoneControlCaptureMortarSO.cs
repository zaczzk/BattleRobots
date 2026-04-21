using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMortar", order = 286)]
    public sealed class ZoneControlCaptureMortarSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _grindsNeeded   = 7;
        [SerializeField, Min(1)] private int _spillPerBot    = 2;
        [SerializeField, Min(0)] private int _bonusPerGrind  = 1030;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMortarGround;

        private int _grinds;
        private int _grindCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   GrindsNeeded      => _grindsNeeded;
        public int   SpillPerBot       => _spillPerBot;
        public int   BonusPerGrind     => _bonusPerGrind;
        public int   Grinds            => _grinds;
        public int   GrindCount        => _grindCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float GrindProgress     => _grindsNeeded > 0
            ? Mathf.Clamp01(_grinds / (float)_grindsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _grinds = Mathf.Min(_grinds + 1, _grindsNeeded);
            if (_grinds >= _grindsNeeded)
            {
                int bonus = _bonusPerGrind;
                _grindCount++;
                _totalBonusAwarded += bonus;
                _grinds             = 0;
                _onMortarGround?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _grinds = Mathf.Max(0, _grinds - _spillPerBot);
        }

        public void Reset()
        {
            _grinds            = 0;
            _grindCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

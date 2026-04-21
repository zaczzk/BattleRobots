using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCrucible", order = 282)]
    public sealed class ZoneControlCaptureCrucibleSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _oreNeeded      = 7;
        [SerializeField, Min(1)] private int _removePerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerAlloy  = 970;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCrucibleAlloyed;

        private int _ore;
        private int _alloyCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   OreNeeded         => _oreNeeded;
        public int   RemovePerBot      => _removePerBot;
        public int   BonusPerAlloy     => _bonusPerAlloy;
        public int   Ore               => _ore;
        public int   AlloyCount        => _alloyCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float OreProgress       => _oreNeeded > 0
            ? Mathf.Clamp01(_ore / (float)_oreNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _ore = Mathf.Min(_ore + 1, _oreNeeded);
            if (_ore >= _oreNeeded)
            {
                int bonus = _bonusPerAlloy;
                _alloyCount++;
                _totalBonusAwarded += bonus;
                _ore                = 0;
                _onCrucibleAlloyed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _ore = Mathf.Max(0, _ore - _removePerBot);
        }

        public void Reset()
        {
            _ore               = 0;
            _alloyCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFurnace", order = 249)]
    public sealed class ZoneControlCaptureFurnaceSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _fuelNeeded      = 5;
        [SerializeField, Min(1)] private int _dousePerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerSmelt   = 435;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFurnaceSmelted;

        private int _fuel;
        private int _smeltCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FuelNeeded        => _fuelNeeded;
        public int   DousePerBot       => _dousePerBot;
        public int   BonusPerSmelt     => _bonusPerSmelt;
        public int   Fuel              => _fuel;
        public int   SmeltCount        => _smeltCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float FuelProgress      => _fuelNeeded > 0
            ? Mathf.Clamp01(_fuel / (float)_fuelNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _fuel = Mathf.Min(_fuel + 1, _fuelNeeded);
            if (_fuel >= _fuelNeeded)
            {
                int bonus = _bonusPerSmelt;
                _smeltCount++;
                _totalBonusAwarded += bonus;
                _fuel               = 0;
                _onFurnaceSmelted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _fuel = Mathf.Max(0, _fuel - _dousePerBot);
        }

        public void Reset()
        {
            _fuel              = 0;
            _smeltCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

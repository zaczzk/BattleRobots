using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCofreeObject", order = 416)]
    public sealed class ZoneControlCaptureCofreeObjectSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _cofactorsNeeded      = 6;
        [SerializeField, Min(1)] private int _dissolvePerBot        = 2;
        [SerializeField, Min(0)] private int _bonusPerCofreeObject  = 2980;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCofreeObjectCofreed;

        private int _cofactors;
        private int _cofreeObjectCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CofactorsNeeded      => _cofactorsNeeded;
        public int   DissolvePerBot       => _dissolvePerBot;
        public int   BonusPerCofreeObject => _bonusPerCofreeObject;
        public int   Cofactors            => _cofactors;
        public int   CofreeObjectCount    => _cofreeObjectCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float CofactorProgress     => _cofactorsNeeded > 0
            ? Mathf.Clamp01(_cofactors / (float)_cofactorsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _cofactors = Mathf.Min(_cofactors + 1, _cofactorsNeeded);
            if (_cofactors >= _cofactorsNeeded)
            {
                int bonus = _bonusPerCofreeObject;
                _cofreeObjectCount++;
                _totalBonusAwarded += bonus;
                _cofactors          = 0;
                _onCofreeObjectCofreed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _cofactors = Mathf.Max(0, _cofactors - _dissolvePerBot);
        }

        public void Reset()
        {
            _cofactors         = 0;
            _cofreeObjectCount = 0;
            _totalBonusAwarded = 0;
        }
    }
}

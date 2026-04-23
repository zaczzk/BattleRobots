using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFreeObject", order = 415)]
    public sealed class ZoneControlCaptureFreeObjectSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _generatorsNeeded   = 5;
        [SerializeField, Min(1)] private int _releasePerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerFreeObject  = 2965;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFreeObjectGenerated;

        private int _generators;
        private int _freeObjectCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   GeneratorsNeeded   => _generatorsNeeded;
        public int   ReleasePerBot      => _releasePerBot;
        public int   BonusPerFreeObject => _bonusPerFreeObject;
        public int   Generators         => _generators;
        public int   FreeObjectCount    => _freeObjectCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float GeneratorProgress  => _generatorsNeeded > 0
            ? Mathf.Clamp01(_generators / (float)_generatorsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _generators = Mathf.Min(_generators + 1, _generatorsNeeded);
            if (_generators >= _generatorsNeeded)
            {
                int bonus = _bonusPerFreeObject;
                _freeObjectCount++;
                _totalBonusAwarded += bonus;
                _generators         = 0;
                _onFreeObjectGenerated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _generators = Mathf.Max(0, _generators - _releasePerBot);
        }

        public void Reset()
        {
            _generators        = 0;
            _freeObjectCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

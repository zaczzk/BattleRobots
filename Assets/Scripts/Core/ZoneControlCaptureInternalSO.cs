using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureInternal", order = 420)]
    public sealed class ZoneControlCaptureInternalSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _objectsNeeded      = 6;
        [SerializeField, Min(1)] private int _destructionPerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerInternal   = 3040;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onInternalCategoryBuilt;

        private int _objects;
        private int _internalCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ObjectsNeeded       => _objectsNeeded;
        public int   DestructionPerBot   => _destructionPerBot;
        public int   BonusPerInternal    => _bonusPerInternal;
        public int   Objects             => _objects;
        public int   InternalCount       => _internalCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float ObjectProgress      => _objectsNeeded > 0
            ? Mathf.Clamp01(_objects / (float)_objectsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _objects = Mathf.Min(_objects + 1, _objectsNeeded);
            if (_objects >= _objectsNeeded)
            {
                int bonus = _bonusPerInternal;
                _internalCount++;
                _totalBonusAwarded += bonus;
                _objects            = 0;
                _onInternalCategoryBuilt?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _objects = Mathf.Max(0, _objects - _destructionPerBot);
        }

        public void Reset()
        {
            _objects           = 0;
            _internalCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

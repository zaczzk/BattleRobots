using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCoproduct", order = 399)]
    public sealed class ZoneControlCaptureCoproductSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _objectsNeeded    = 5;
        [SerializeField, Min(1)] private int _collapsePerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerCoproduct = 2725;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCoproductInjected;

        private int _objects;
        private int _coproductCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ObjectsNeeded       => _objectsNeeded;
        public int   CollapsePerBot      => _collapsePerBot;
        public int   BonusPerCoproduct   => _bonusPerCoproduct;
        public int   Objects             => _objects;
        public int   CoproductCount      => _coproductCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float ObjectProgress      => _objectsNeeded > 0
            ? Mathf.Clamp01(_objects / (float)_objectsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _objects = Mathf.Min(_objects + 1, _objectsNeeded);
            if (_objects >= _objectsNeeded)
            {
                int bonus = _bonusPerCoproduct;
                _coproductCount++;
                _totalBonusAwarded += bonus;
                _objects            = 0;
                _onCoproductInjected?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _objects = Mathf.Max(0, _objects - _collapsePerBot);
        }

        public void Reset()
        {
            _objects           = 0;
            _coproductCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}

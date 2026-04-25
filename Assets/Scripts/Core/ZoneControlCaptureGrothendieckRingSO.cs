using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureGrothendieckRing", order = 494)]
    public sealed class ZoneControlCaptureGrothendieckRingSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _virtualObjectsNeeded   = 6;
        [SerializeField, Min(1)] private int _exactTrianglesPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerAddition       = 4150;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onGrothendieckRingAdded;

        private int _virtualObjects;
        private int _additionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   VirtualObjectsNeeded => _virtualObjectsNeeded;
        public int   ExactTrianglesPerBot => _exactTrianglesPerBot;
        public int   BonusPerAddition     => _bonusPerAddition;
        public int   VirtualObjects       => _virtualObjects;
        public int   AdditionCount        => _additionCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float VirtualObjectProgress => _virtualObjectsNeeded > 0
            ? Mathf.Clamp01(_virtualObjects / (float)_virtualObjectsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _virtualObjects = Mathf.Min(_virtualObjects + 1, _virtualObjectsNeeded);
            if (_virtualObjects >= _virtualObjectsNeeded)
            {
                int bonus = _bonusPerAddition;
                _additionCount++;
                _totalBonusAwarded += bonus;
                _virtualObjects     = 0;
                _onGrothendieckRingAdded?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _virtualObjects = Mathf.Max(0, _virtualObjects - _exactTrianglesPerBot);
        }

        public void Reset()
        {
            _virtualObjects    = 0;
            _additionCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

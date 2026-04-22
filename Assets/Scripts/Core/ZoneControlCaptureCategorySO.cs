using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCategory", order = 373)]
    public sealed class ZoneControlCaptureCategorySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _objectsNeeded      = 5;
        [SerializeField, Min(1)] private int _disconnectPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerMorphism   = 2335;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMorphismFormed;

        private int _objects;
        private int _morphismCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ObjectsNeeded      => _objectsNeeded;
        public int   DisconnectPerBot   => _disconnectPerBot;
        public int   BonusPerMorphism   => _bonusPerMorphism;
        public int   Objects            => _objects;
        public int   MorphismCount      => _morphismCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ObjectProgress     => _objectsNeeded > 0
            ? Mathf.Clamp01(_objects / (float)_objectsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _objects = Mathf.Min(_objects + 1, _objectsNeeded);
            if (_objects >= _objectsNeeded)
            {
                int bonus = _bonusPerMorphism;
                _morphismCount++;
                _totalBonusAwarded += bonus;
                _objects            = 0;
                _onMorphismFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _objects = Mathf.Max(0, _objects - _disconnectPerBot);
        }

        public void Reset()
        {
            _objects           = 0;
            _morphismCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

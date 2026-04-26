using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePolymorphicType", order = 559)]
    public sealed class ZoneControlCapturePolymorphicTypeSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _typeInstantiationsNeeded  = 6;
        [SerializeField, Min(1)] private int _typeVariableClashesPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerInstantiation     = 5125;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPolymorphicTypeCompleted;

        private int _typeInstantiations;
        private int _instantiationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   TypeInstantiationsNeeded  => _typeInstantiationsNeeded;
        public int   TypeVariableClashesPerBot => _typeVariableClashesPerBot;
        public int   BonusPerInstantiation     => _bonusPerInstantiation;
        public int   TypeInstantiations        => _typeInstantiations;
        public int   InstantiationCount        => _instantiationCount;
        public int   TotalBonusAwarded         => _totalBonusAwarded;
        public float TypeInstantiationProgress => _typeInstantiationsNeeded > 0
            ? Mathf.Clamp01(_typeInstantiations / (float)_typeInstantiationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _typeInstantiations = Mathf.Min(_typeInstantiations + 1, _typeInstantiationsNeeded);
            if (_typeInstantiations >= _typeInstantiationsNeeded)
            {
                int bonus = _bonusPerInstantiation;
                _instantiationCount++;
                _totalBonusAwarded  += bonus;
                _typeInstantiations  = 0;
                _onPolymorphicTypeCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _typeInstantiations = Mathf.Max(0, _typeInstantiations - _typeVariableClashesPerBot);
        }

        public void Reset()
        {
            _typeInstantiations = 0;
            _instantiationCount = 0;
            _totalBonusAwarded  = 0;
        }
    }
}

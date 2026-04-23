using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureNatural", order = 413)]
    public sealed class ZoneControlCaptureNaturalSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _componentsNeeded      = 7;
        [SerializeField, Min(1)] private int _perturbPerBot         = 2;
        [SerializeField, Min(0)] private int _bonusPerTransformation = 2935;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNaturalTransformed;

        private int _components;
        private int _transformationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ComponentsNeeded       => _componentsNeeded;
        public int   PerturbPerBot          => _perturbPerBot;
        public int   BonusPerTransformation => _bonusPerTransformation;
        public int   Components             => _components;
        public int   TransformationCount    => _transformationCount;
        public int   TotalBonusAwarded      => _totalBonusAwarded;
        public float ComponentProgress      => _componentsNeeded > 0
            ? Mathf.Clamp01(_components / (float)_componentsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _components = Mathf.Min(_components + 1, _componentsNeeded);
            if (_components >= _componentsNeeded)
            {
                int bonus = _bonusPerTransformation;
                _transformationCount++;
                _totalBonusAwarded += bonus;
                _components         = 0;
                _onNaturalTransformed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _components = Mathf.Max(0, _components - _perturbPerBot);
        }

        public void Reset()
        {
            _components          = 0;
            _transformationCount = 0;
            _totalBonusAwarded   = 0;
        }
    }
}

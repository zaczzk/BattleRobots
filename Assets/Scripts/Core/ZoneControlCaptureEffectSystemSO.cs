using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureEffectSystem", order = 566)]
    public sealed class ZoneControlCaptureEffectSystemSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _pureEffectAnnotationsNeeded = 6;
        [SerializeField, Min(1)] private int _effectLeaksPerBot           = 1;
        [SerializeField, Min(0)] private int _bonusPerAnnotation          = 5230;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onEffectSystemCompleted;

        private int _pureEffectAnnotations;
        private int _annotationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PureEffectAnnotationsNeeded => _pureEffectAnnotationsNeeded;
        public int   EffectLeaksPerBot           => _effectLeaksPerBot;
        public int   BonusPerAnnotation          => _bonusPerAnnotation;
        public int   PureEffectAnnotations       => _pureEffectAnnotations;
        public int   AnnotationCount             => _annotationCount;
        public int   TotalBonusAwarded           => _totalBonusAwarded;
        public float PureEffectAnnotationProgress => _pureEffectAnnotationsNeeded > 0
            ? Mathf.Clamp01(_pureEffectAnnotations / (float)_pureEffectAnnotationsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _pureEffectAnnotations = Mathf.Min(_pureEffectAnnotations + 1, _pureEffectAnnotationsNeeded);
            if (_pureEffectAnnotations >= _pureEffectAnnotationsNeeded)
            {
                int bonus = _bonusPerAnnotation;
                _annotationCount++;
                _totalBonusAwarded      += bonus;
                _pureEffectAnnotations   = 0;
                _onEffectSystemCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _pureEffectAnnotations = Mathf.Max(0, _pureEffectAnnotations - _effectLeaksPerBot);
        }

        public void Reset()
        {
            _pureEffectAnnotations = 0;
            _annotationCount       = 0;
            _totalBonusAwarded     = 0;
        }
    }
}

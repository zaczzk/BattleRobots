using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCobordismCategory", order = 492)]
    public sealed class ZoneControlCaptureCobordismCategorySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _bordismsNeeded        = 5;
        [SerializeField, Min(1)] private int _singularitiesPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerComposition   = 4120;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCobordismCategoryComposed;

        private int _bordisms;
        private int _compositionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BordismsNeeded       => _bordismsNeeded;
        public int   SingularitiesPerBot  => _singularitiesPerBot;
        public int   BonusPerComposition  => _bonusPerComposition;
        public int   Bordisms             => _bordisms;
        public int   CompositionCount     => _compositionCount;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float BordismProgress      => _bordismsNeeded > 0
            ? Mathf.Clamp01(_bordisms / (float)_bordismsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _bordisms = Mathf.Min(_bordisms + 1, _bordismsNeeded);
            if (_bordisms >= _bordismsNeeded)
            {
                int bonus = _bonusPerComposition;
                _compositionCount++;
                _totalBonusAwarded += bonus;
                _bordisms           = 0;
                _onCobordismCategoryComposed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _bordisms = Mathf.Max(0, _bordisms - _singularitiesPerBot);
        }

        public void Reset()
        {
            _bordisms          = 0;
            _compositionCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

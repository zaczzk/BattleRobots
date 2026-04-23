using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureInitial", order = 409)]
    public sealed class ZoneControlCaptureInitialSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _morphismsNeeded = 7;
        [SerializeField, Min(1)] private int _voidPerBot      = 2;
        [SerializeField, Min(0)] private int _bonusPerInitial = 2875;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onInitialInjected;

        private int _morphisms;
        private int _initialCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   MorphismsNeeded   => _morphismsNeeded;
        public int   VoidPerBot        => _voidPerBot;
        public int   BonusPerInitial   => _bonusPerInitial;
        public int   Morphisms         => _morphisms;
        public int   InitialCount      => _initialCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float MorphismProgress  => _morphismsNeeded > 0
            ? Mathf.Clamp01(_morphisms / (float)_morphismsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _morphisms = Mathf.Min(_morphisms + 1, _morphismsNeeded);
            if (_morphisms >= _morphismsNeeded)
            {
                int bonus = _bonusPerInitial;
                _initialCount++;
                _totalBonusAwarded += bonus;
                _morphisms          = 0;
                _onInitialInjected?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _morphisms = Mathf.Max(0, _morphisms - _voidPerBot);
        }

        public void Reset()
        {
            _morphisms         = 0;
            _initialCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

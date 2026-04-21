using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePrism", order = 275)]
    public sealed class ZoneControlCapturePrismSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _fragmentsNeeded = 5;
        [SerializeField, Min(1)] private int _splitPerBot     = 1;
        [SerializeField, Min(0)] private int _bonusPerRefraction = 865;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPrismRefracted;

        private int _fragments;
        private int _refractionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FragmentsNeeded     => _fragmentsNeeded;
        public int   SplitPerBot         => _splitPerBot;
        public int   BonusPerRefraction  => _bonusPerRefraction;
        public int   Fragments           => _fragments;
        public int   RefractionCount     => _refractionCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float FragmentProgress    => _fragmentsNeeded > 0
            ? Mathf.Clamp01(_fragments / (float)_fragmentsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _fragments = Mathf.Min(_fragments + 1, _fragmentsNeeded);
            if (_fragments >= _fragmentsNeeded)
            {
                int bonus = _bonusPerRefraction;
                _refractionCount++;
                _totalBonusAwarded += bonus;
                _fragments          = 0;
                _onPrismRefracted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _fragments = Mathf.Max(0, _fragments - _splitPerBot);
        }

        public void Reset()
        {
            _fragments         = 0;
            _refractionCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

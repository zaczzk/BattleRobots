using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRelic", order = 238)]
    public sealed class ZoneControlCaptureRelicSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _fragmentsNeeded     = 5;
        [SerializeField, Min(1)] private int _damagePerBot        = 1;
        [SerializeField, Min(0)] private int _bonusPerRestoration = 475;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRelicRestored;

        private int _fragments;
        private int _restorationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   FragmentsNeeded     => _fragmentsNeeded;
        public int   DamagePerBot        => _damagePerBot;
        public int   BonusPerRestoration => _bonusPerRestoration;
        public int   Fragments           => _fragments;
        public int   RestorationCount    => _restorationCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float FragmentProgress    => _fragmentsNeeded > 0
            ? Mathf.Clamp01(_fragments / (float)_fragmentsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _fragments = Mathf.Min(_fragments + 1, _fragmentsNeeded);
            if (_fragments >= _fragmentsNeeded)
            {
                int bonus = _bonusPerRestoration;
                _restorationCount++;
                _totalBonusAwarded += bonus;
                _fragments          = 0;
                _onRelicRestored?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _fragments = Mathf.Max(0, _fragments - _damagePerBot);
        }

        public void Reset()
        {
            _fragments         = 0;
            _restorationCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

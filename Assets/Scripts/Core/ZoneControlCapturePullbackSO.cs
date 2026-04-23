using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePullback", order = 401)]
    public sealed class ZoneControlCapturePullbackSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _morphismsNeeded  = 5;
        [SerializeField, Min(1)] private int _unravelPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerPullback = 2755;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPullbackPulled;

        private int _morphisms;
        private int _pullbackCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   MorphismsNeeded   => _morphismsNeeded;
        public int   UnravelPerBot     => _unravelPerBot;
        public int   BonusPerPullback  => _bonusPerPullback;
        public int   Morphisms         => _morphisms;
        public int   PullbackCount     => _pullbackCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float MorphismProgress  => _morphismsNeeded > 0
            ? Mathf.Clamp01(_morphisms / (float)_morphismsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _morphisms = Mathf.Min(_morphisms + 1, _morphismsNeeded);
            if (_morphisms >= _morphismsNeeded)
            {
                int bonus = _bonusPerPullback;
                _pullbackCount++;
                _totalBonusAwarded += bonus;
                _morphisms          = 0;
                _onPullbackPulled?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _morphisms = Mathf.Max(0, _morphisms - _unravelPerBot);
        }

        public void Reset()
        {
            _morphisms         = 0;
            _pullbackCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

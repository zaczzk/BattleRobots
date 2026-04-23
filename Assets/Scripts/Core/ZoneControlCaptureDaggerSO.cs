using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureDagger", order = 422)]
    public sealed class ZoneControlCaptureDaggerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _edgesNeeded    = 5;
        [SerializeField, Min(1)] private int _reversePerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerDagger = 3070;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onDaggerFormed;

        private int _edges;
        private int _daggerCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   EdgesNeeded       => _edgesNeeded;
        public int   ReversePerBot     => _reversePerBot;
        public int   BonusPerDagger    => _bonusPerDagger;
        public int   Edges             => _edges;
        public int   DaggerCount       => _daggerCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float EdgeProgress      => _edgesNeeded > 0
            ? Mathf.Clamp01(_edges / (float)_edgesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _edges = Mathf.Min(_edges + 1, _edgesNeeded);
            if (_edges >= _edgesNeeded)
            {
                int bonus = _bonusPerDagger;
                _daggerCount++;
                _totalBonusAwarded += bonus;
                _edges              = 0;
                _onDaggerFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _edges = Mathf.Max(0, _edges - _reversePerBot);
        }

        public void Reset()
        {
            _edges             = 0;
            _daggerCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureHomotopyTypeTheory", order = 553)]
    public sealed class ZoneControlCaptureHomotopyTypeTheorySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _pathEquivalencesNeeded          = 6;
        [SerializeField, Min(1)] private int _nonUnivalentObstructionsPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerUnivalence              = 5035;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onHomotopyTypeTheoryCompleted;

        private int _pathEquivalences;
        private int _univalenceCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PathEquivalencesNeeded         => _pathEquivalencesNeeded;
        public int   NonUnivalentObstructionsPerBot => _nonUnivalentObstructionsPerBot;
        public int   BonusPerUnivalence             => _bonusPerUnivalence;
        public int   PathEquivalences               => _pathEquivalences;
        public int   UnivalenceCount                => _univalenceCount;
        public int   TotalBonusAwarded              => _totalBonusAwarded;
        public float PathEquivalenceProgress => _pathEquivalencesNeeded > 0
            ? Mathf.Clamp01(_pathEquivalences / (float)_pathEquivalencesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _pathEquivalences = Mathf.Min(_pathEquivalences + 1, _pathEquivalencesNeeded);
            if (_pathEquivalences >= _pathEquivalencesNeeded)
            {
                int bonus = _bonusPerUnivalence;
                _univalenceCount++;
                _totalBonusAwarded += bonus;
                _pathEquivalences   = 0;
                _onHomotopyTypeTheoryCompleted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _pathEquivalences = Mathf.Max(0, _pathEquivalences - _nonUnivalentObstructionsPerBot);
        }

        public void Reset()
        {
            _pathEquivalences  = 0;
            _univalenceCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

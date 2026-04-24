using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureNucleus", order = 436)]
    public sealed class ZoneControlCaptureNucleusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _morphismsNeeded  = 6;
        [SerializeField, Min(1)] private int _contractPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerClosure  = 3280;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNucleusClosed;

        private int _morphisms;
        private int _closureCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   MorphismsNeeded   => _morphismsNeeded;
        public int   ContractPerBot    => _contractPerBot;
        public int   BonusPerClosure   => _bonusPerClosure;
        public int   Morphisms         => _morphisms;
        public int   ClosureCount      => _closureCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float MorphismProgress  => _morphismsNeeded > 0
            ? Mathf.Clamp01(_morphisms / (float)_morphismsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _morphisms = Mathf.Min(_morphisms + 1, _morphismsNeeded);
            if (_morphisms >= _morphismsNeeded)
            {
                int bonus = _bonusPerClosure;
                _closureCount++;
                _totalBonusAwarded += bonus;
                _morphisms          = 0;
                _onNucleusClosed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _morphisms = Mathf.Max(0, _morphisms - _contractPerBot);
        }

        public void Reset()
        {
            _morphisms         = 0;
            _closureCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

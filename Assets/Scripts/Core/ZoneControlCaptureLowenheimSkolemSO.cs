using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLowenheimSkolem", order = 542)]
    public sealed class ZoneControlCaptureLowenheimSkolemSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _downwardWitnessesNeeded      = 6;
        [SerializeField, Min(1)] private int _uncountableObstructionsPerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerWitnessing            = 4870;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLowenheimSkolemWitnessed;

        private int _downwardWitnesses;
        private int _witnessingCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   DownwardWitnessesNeeded      => _downwardWitnessesNeeded;
        public int   UncountableObstructionsPerBot => _uncountableObstructionsPerBot;
        public int   BonusPerWitnessing            => _bonusPerWitnessing;
        public int   DownwardWitnesses             => _downwardWitnesses;
        public int   WitnessingCount               => _witnessingCount;
        public int   TotalBonusAwarded             => _totalBonusAwarded;
        public float DownwardWitnessProgress       => _downwardWitnessesNeeded > 0
            ? Mathf.Clamp01(_downwardWitnesses / (float)_downwardWitnessesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _downwardWitnesses = Mathf.Min(_downwardWitnesses + 1, _downwardWitnessesNeeded);
            if (_downwardWitnesses >= _downwardWitnessesNeeded)
            {
                int bonus = _bonusPerWitnessing;
                _witnessingCount++;
                _totalBonusAwarded += bonus;
                _downwardWitnesses  = 0;
                _onLowenheimSkolemWitnessed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _downwardWitnesses = Mathf.Max(0, _downwardWitnesses - _uncountableObstructionsPerBot);
        }

        public void Reset()
        {
            _downwardWitnesses = 0;
            _witnessingCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

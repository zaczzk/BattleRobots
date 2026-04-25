using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureFermatLastTheorem", order = 529)]
    public sealed class ZoneControlCaptureFermatLastTheoremSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _modularFormsNeeded         = 7;
        [SerializeField, Min(1)] private int _faltingsObstructionsPerBot = 2;
        [SerializeField, Min(0)] private int _bonusPerProof              = 4675;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onFermatLastTheoremProved;

        private int _modularForms;
        private int _proofCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ModularFormsNeeded         => _modularFormsNeeded;
        public int   FaltingsObstructionsPerBot => _faltingsObstructionsPerBot;
        public int   BonusPerProof              => _bonusPerProof;
        public int   ModularForms               => _modularForms;
        public int   ProofCount                 => _proofCount;
        public int   TotalBonusAwarded          => _totalBonusAwarded;
        public float ModularFormProgress        => _modularFormsNeeded > 0
            ? Mathf.Clamp01(_modularForms / (float)_modularFormsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _modularForms = Mathf.Min(_modularForms + 1, _modularFormsNeeded);
            if (_modularForms >= _modularFormsNeeded)
            {
                int bonus = _bonusPerProof;
                _proofCount++;
                _totalBonusAwarded += bonus;
                _modularForms       = 0;
                _onFermatLastTheoremProved?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _modularForms = Mathf.Max(0, _modularForms - _faltingsObstructionsPerBot);
        }

        public void Reset()
        {
            _modularForms      = 0;
            _proofCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMedallion", order = 265)]
    public sealed class ZoneControlCaptureMedallionSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _inscriptionsNeeded  = 5;
        [SerializeField, Min(1)] private int _erasePerBot         = 1;
        [SerializeField, Min(0)] private int _bonusPerMedallion   = 715;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMedallionComplete;

        private int _inscriptions;
        private int _medallionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   InscriptionsNeeded => _inscriptionsNeeded;
        public int   ErasePerBot        => _erasePerBot;
        public int   BonusPerMedallion  => _bonusPerMedallion;
        public int   Inscriptions       => _inscriptions;
        public int   MedallionCount     => _medallionCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float InscriptionProgress => _inscriptionsNeeded > 0
            ? Mathf.Clamp01(_inscriptions / (float)_inscriptionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _inscriptions = Mathf.Min(_inscriptions + 1, _inscriptionsNeeded);
            if (_inscriptions >= _inscriptionsNeeded)
            {
                int bonus = _bonusPerMedallion;
                _medallionCount++;
                _totalBonusAwarded += bonus;
                _inscriptions       = 0;
                _onMedallionComplete?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _inscriptions = Mathf.Max(0, _inscriptions - _erasePerBot);
        }

        public void Reset()
        {
            _inscriptions      = 0;
            _medallionCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}

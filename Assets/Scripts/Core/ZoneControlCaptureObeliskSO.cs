using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureObelisk", order = 243)]
    public sealed class ZoneControlCaptureObeliskSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _runesNeeded    = 5;
        [SerializeField, Min(1)] private int _erosionPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerInscription = 510;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onObeliskInscribed;

        private int _runes;
        private int _inscriptionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   RunesNeeded        => _runesNeeded;
        public int   ErosionPerBot      => _erosionPerBot;
        public int   BonusPerInscription => _bonusPerInscription;
        public int   Runes              => _runes;
        public int   InscriptionCount   => _inscriptionCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float RuneProgress       => _runesNeeded > 0
            ? Mathf.Clamp01(_runes / (float)_runesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _runes = Mathf.Min(_runes + 1, _runesNeeded);
            if (_runes >= _runesNeeded)
            {
                int bonus = _bonusPerInscription;
                _inscriptionCount++;
                _totalBonusAwarded += bonus;
                _runes              = 0;
                _onObeliskInscribed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _runes = Mathf.Max(0, _runes - _erosionPerBot);
        }

        public void Reset()
        {
            _runes             = 0;
            _inscriptionCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

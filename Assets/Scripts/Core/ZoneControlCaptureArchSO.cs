using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureArch", order = 248)]
    public sealed class ZoneControlCaptureArchSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _keystonesNeeded  = 6;
        [SerializeField, Min(1)] private int _topplePerBot     = 2;
        [SerializeField, Min(0)] private int _bonusPerArch     = 455;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onArchComplete;

        private int _keystones;
        private int _archCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   KeystonesNeeded   => _keystonesNeeded;
        public int   TopplePerBot      => _topplePerBot;
        public int   BonusPerArch      => _bonusPerArch;
        public int   Keystones         => _keystones;
        public int   ArchCount         => _archCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float KeystoneProgress  => _keystonesNeeded > 0
            ? Mathf.Clamp01(_keystones / (float)_keystonesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _keystones = Mathf.Min(_keystones + 1, _keystonesNeeded);
            if (_keystones >= _keystonesNeeded)
            {
                int bonus = _bonusPerArch;
                _archCount++;
                _totalBonusAwarded += bonus;
                _keystones          = 0;
                _onArchComplete?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _keystones = Mathf.Max(0, _keystones - _topplePerBot);
        }

        public void Reset()
        {
            _keystones         = 0;
            _archCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

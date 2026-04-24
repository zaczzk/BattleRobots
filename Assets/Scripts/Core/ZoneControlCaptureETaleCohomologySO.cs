using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureETaleCohomology", order = 472)]
    public sealed class ZoneControlCaptureETaleCohomologySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _stalksNeeded            = 6;
        [SerializeField, Min(1)] private int _breakPerBot             = 2;
        [SerializeField, Min(0)] private int _bonusPerSheafification  = 3820;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onETaleSheafified;

        private int _stalks;
        private int _sheafifyCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StalksNeeded           => _stalksNeeded;
        public int   BreakPerBot            => _breakPerBot;
        public int   BonusPerSheafification => _bonusPerSheafification;
        public int   Stalks                 => _stalks;
        public int   SheafifyCount          => _sheafifyCount;
        public int   TotalBonusAwarded      => _totalBonusAwarded;
        public float StalkProgress          => _stalksNeeded > 0
            ? Mathf.Clamp01(_stalks / (float)_stalksNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _stalks = Mathf.Min(_stalks + 1, _stalksNeeded);
            if (_stalks >= _stalksNeeded)
            {
                int bonus = _bonusPerSheafification;
                _sheafifyCount++;
                _totalBonusAwarded += bonus;
                _stalks             = 0;
                _onETaleSheafified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _stalks = Mathf.Max(0, _stalks - _breakPerBot);
        }

        public void Reset()
        {
            _stalks            = 0;
            _sheafifyCount     = 0;
            _totalBonusAwarded = 0;
        }
    }
}

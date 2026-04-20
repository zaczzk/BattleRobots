using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCrystal", order = 219)]
    public sealed class ZoneControlCaptureCrystalSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _capturesForShatter  = 5;
        [SerializeField, Min(1)] private int _cracksPerBotCapture = 1;
        [SerializeField, Min(0)] private int _bonusPerShatter     = 320;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onShatter;

        private int _crystalGrowth;
        private int _shatterCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CapturesForShatter  => _capturesForShatter;
        public int   CracksPerBotCapture => _cracksPerBotCapture;
        public int   BonusPerShatter     => _bonusPerShatter;
        public int   CrystalGrowth       => _crystalGrowth;
        public int   ShatterCount        => _shatterCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float CrystalProgress     => _capturesForShatter > 0
            ? Mathf.Clamp01(_crystalGrowth / (float)_capturesForShatter)
            : 0f;

        public int RecordPlayerCapture()
        {
            _crystalGrowth++;
            if (_crystalGrowth >= _capturesForShatter)
            {
                Shatter();
                return _bonusPerShatter;
            }
            return 0;
        }

        private void Shatter()
        {
            _shatterCount++;
            _totalBonusAwarded += _bonusPerShatter;
            _crystalGrowth      = 0;
            _onShatter?.Raise();
        }

        public void RecordBotCapture()
        {
            _crystalGrowth = Mathf.Max(0, _crystalGrowth - _cracksPerBotCapture);
        }

        public void Reset()
        {
            _crystalGrowth     = 0;
            _shatterCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

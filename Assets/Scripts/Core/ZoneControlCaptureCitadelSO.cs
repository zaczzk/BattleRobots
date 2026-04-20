using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCitadel", order = 214)]
    public sealed class ZoneControlCaptureCitadelSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _capturesForCitadel    = 4;
        [SerializeField, Min(0)] private int _bonusPerCaptureWithin = 100;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCitadelBuilt;
        [SerializeField] private VoidGameEvent _onCitadelDemolished;

        private int  _buildCount;
        private bool _isCitadelBuilt;
        private int  _citadelCount;
        private int  _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CapturesForCitadel    => _capturesForCitadel;
        public int   BonusPerCaptureWithin => _bonusPerCaptureWithin;
        public int   BuildCount            => _buildCount;
        public bool  IsCitadelBuilt        => _isCitadelBuilt;
        public int   CitadelCount          => _citadelCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float BuildProgress         => _capturesForCitadel > 0
            ? Mathf.Clamp01(_buildCount / (float)_capturesForCitadel)
            : 0f;

        public int RecordPlayerCapture()
        {
            if (_isCitadelBuilt)
            {
                int bonus = _bonusPerCaptureWithin;
                _totalBonusAwarded += bonus;
                return bonus;
            }

            _buildCount++;
            if (_buildCount >= _capturesForCitadel)
                Build();
            return 0;
        }

        private void Build()
        {
            _isCitadelBuilt = true;
            _buildCount     = 0;
            _citadelCount++;
            _onCitadelBuilt?.Raise();
        }

        public void RecordBotCapture()
        {
            if (_isCitadelBuilt)
            {
                Demolish();
            }
            else
            {
                _buildCount = Mathf.Max(0, _buildCount - 1);
            }
        }

        private void Demolish()
        {
            _isCitadelBuilt = false;
            _buildCount     = 0;
            _onCitadelDemolished?.Raise();
        }

        public void Reset()
        {
            _buildCount        = 0;
            _isCitadelBuilt    = false;
            _citadelCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

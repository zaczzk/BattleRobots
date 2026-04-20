using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureVortex", order = 220)]
    public sealed class ZoneControlCaptureVortexSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _chargesForVortex        = 4;
        [SerializeField, Min(1)] private int _vortexDurationCaptures  = 3;
        [SerializeField, Min(0)] private int _bonusPerVortexCapture   = 110;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onVortexOpened;
        [SerializeField] private VoidGameEvent _onVortexClosed;

        private int  _botChargeCount;
        private bool _isActive;
        private int  _capturesRemaining;
        private int  _vortexCount;
        private int  _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ChargesForVortex       => _chargesForVortex;
        public int   VortexDurationCaptures => _vortexDurationCaptures;
        public int   BonusPerVortexCapture  => _bonusPerVortexCapture;
        public int   BotChargeCount         => _botChargeCount;
        public bool  IsActive               => _isActive;
        public int   CapturesRemaining      => _capturesRemaining;
        public int   VortexCount            => _vortexCount;
        public int   TotalBonusAwarded      => _totalBonusAwarded;
        public float VortexProgress         => _isActive
            ? Mathf.Clamp01(_capturesRemaining / (float)_vortexDurationCaptures)
            : (_chargesForVortex > 0 ? Mathf.Clamp01(_botChargeCount / (float)_chargesForVortex) : 0f);

        public void RecordBotCapture()
        {
            if (_isActive)
            {
                CloseVortex();
                return;
            }
            _botChargeCount++;
            if (_botChargeCount >= _chargesForVortex)
                OpenVortex();
        }

        private void OpenVortex()
        {
            _isActive          = true;
            _capturesRemaining = _vortexDurationCaptures;
            _vortexCount++;
            _botChargeCount    = 0;
            _onVortexOpened?.Raise();
        }

        public int RecordPlayerCapture()
        {
            if (!_isActive)
                return 0;
            _capturesRemaining--;
            _totalBonusAwarded += _bonusPerVortexCapture;
            if (_capturesRemaining <= 0)
                CloseVortex();
            return _bonusPerVortexCapture;
        }

        private void CloseVortex()
        {
            _isActive          = false;
            _capturesRemaining = 0;
            _onVortexClosed?.Raise();
        }

        public void Reset()
        {
            _botChargeCount    = 0;
            _isActive          = false;
            _capturesRemaining = 0;
            _vortexCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSigil", order = 218)]
    public sealed class ZoneControlCaptureSigilSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _capturesForSigil  = 6;
        [SerializeField, Min(1)] private int _botBreakThreshold = 2;
        [SerializeField, Min(0)] private int _bonusPerSigil     = 350;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSigilAwakened;

        private int _sigilCharges;
        private int _botCapturesSinceBreak;
        private int _sigilCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   CapturesForSigil       => _capturesForSigil;
        public int   BotBreakThreshold      => _botBreakThreshold;
        public int   BonusPerSigil          => _bonusPerSigil;
        public int   SigilCharges           => _sigilCharges;
        public int   BotCapturesSinceBreak  => _botCapturesSinceBreak;
        public int   SigilCount             => _sigilCount;
        public int   TotalBonusAwarded      => _totalBonusAwarded;
        public float SigilProgress          => _capturesForSigil > 0
            ? Mathf.Clamp01(_sigilCharges / (float)_capturesForSigil)
            : 0f;

        public int RecordPlayerCapture()
        {
            _botCapturesSinceBreak = 0;
            _sigilCharges++;
            if (_sigilCharges >= _capturesForSigil)
            {
                Awaken();
                return _bonusPerSigil;
            }
            return 0;
        }

        private void Awaken()
        {
            _sigilCount++;
            _totalBonusAwarded     += _bonusPerSigil;
            _sigilCharges           = 0;
            _botCapturesSinceBreak  = 0;
            _onSigilAwakened?.Raise();
        }

        public void RecordBotCapture()
        {
            _botCapturesSinceBreak++;
            if (_botCapturesSinceBreak >= _botBreakThreshold)
            {
                _botCapturesSinceBreak = 0;
                _sigilCharges          = Mathf.Max(0, _sigilCharges - 1);
            }
        }

        public void Reset()
        {
            _sigilCharges          = 0;
            _botCapturesSinceBreak = 0;
            _sigilCount            = 0;
            _totalBonusAwarded     = 0;
        }
    }
}

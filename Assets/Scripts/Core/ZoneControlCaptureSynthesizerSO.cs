using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSynthesizer", order = 332)]
    public sealed class ZoneControlCaptureSynthesizerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _voicesNeeded  = 5;
        [SerializeField, Min(1)] private int _resetPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerSynth = 1720;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSynthesizerPlayed;

        private int _voices;
        private int _synthCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   VoicesNeeded      => _voicesNeeded;
        public int   ResetPerBot       => _resetPerBot;
        public int   BonusPerSynth     => _bonusPerSynth;
        public int   Voices            => _voices;
        public int   SynthCount        => _synthCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float VoiceProgress     => _voicesNeeded > 0
            ? Mathf.Clamp01(_voices / (float)_voicesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _voices = Mathf.Min(_voices + 1, _voicesNeeded);
            if (_voices >= _voicesNeeded)
            {
                int bonus = _bonusPerSynth;
                _synthCount++;
                _totalBonusAwarded += bonus;
                _voices             = 0;
                _onSynthesizerPlayed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _voices = Mathf.Max(0, _voices - _resetPerBot);
        }

        public void Reset()
        {
            _voices            = 0;
            _synthCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

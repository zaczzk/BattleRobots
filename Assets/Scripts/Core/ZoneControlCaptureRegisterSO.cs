using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureRegister", order = 341)]
    public sealed class ZoneControlCaptureRegisterSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _wordsNeeded    = 4;
        [SerializeField, Min(1)] private int _clearPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerWrite  = 1855;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onRegisterWritten;

        private int _words;
        private int _writeCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   WordsNeeded       => _wordsNeeded;
        public int   ClearPerBot       => _clearPerBot;
        public int   BonusPerWrite     => _bonusPerWrite;
        public int   Words             => _words;
        public int   WriteCount        => _writeCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float WordProgress      => _wordsNeeded > 0
            ? Mathf.Clamp01(_words / (float)_wordsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _words = Mathf.Min(_words + 1, _wordsNeeded);
            if (_words >= _wordsNeeded)
            {
                int bonus = _bonusPerWrite;
                _writeCount++;
                _totalBonusAwarded += bonus;
                _words              = 0;
                _onRegisterWritten?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _words = Mathf.Max(0, _words - _clearPerBot);
        }

        public void Reset()
        {
            _words             = 0;
            _writeCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

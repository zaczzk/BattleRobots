using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCoequalizer", order = 404)]
    public sealed class ZoneControlCaptureCoequalizerSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _classesNeeded       = 5;
        [SerializeField, Min(1)] private int _dissolvePerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerCoequalizer = 2800;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCoequalizerFormed;

        private int _classes;
        private int _coequalizerCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ClassesNeeded       => _classesNeeded;
        public int   DissolvePerBot      => _dissolvePerBot;
        public int   BonusPerCoequalizer => _bonusPerCoequalizer;
        public int   Classes             => _classes;
        public int   CoequalizerCount    => _coequalizerCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float ClassProgress       => _classesNeeded > 0
            ? Mathf.Clamp01(_classes / (float)_classesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _classes = Mathf.Min(_classes + 1, _classesNeeded);
            if (_classes >= _classesNeeded)
            {
                int bonus = _bonusPerCoequalizer;
                _coequalizerCount++;
                _totalBonusAwarded += bonus;
                _classes            = 0;
                _onCoequalizerFormed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _classes = Mathf.Max(0, _classes - _dissolvePerBot);
        }

        public void Reset()
        {
            _classes           = 0;
            _coequalizerCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

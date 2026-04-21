using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureQuill", order = 272)]
    public sealed class ZoneControlCaptureQuillSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _strokesNeeded      = 5;
        [SerializeField, Min(1)] private int _blotPerBot         = 1;
        [SerializeField, Min(0)] private int _bonusPerInscription = 820;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onQuillInscribed;

        private int _strokes;
        private int _inscriptionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StrokesNeeded       => _strokesNeeded;
        public int   BlotPerBot          => _blotPerBot;
        public int   BonusPerInscription => _bonusPerInscription;
        public int   Strokes             => _strokes;
        public int   InscriptionCount    => _inscriptionCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float StrokeProgress      => _strokesNeeded > 0
            ? Mathf.Clamp01(_strokes / (float)_strokesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _strokes = Mathf.Min(_strokes + 1, _strokesNeeded);
            if (_strokes >= _strokesNeeded)
            {
                int bonus = _bonusPerInscription;
                _inscriptionCount++;
                _totalBonusAwarded += bonus;
                _strokes            = 0;
                _onQuillInscribed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _strokes = Mathf.Max(0, _strokes - _blotPerBot);
        }

        public void Reset()
        {
            _strokes           = 0;
            _inscriptionCount  = 0;
            _totalBonusAwarded = 0;
        }
    }
}

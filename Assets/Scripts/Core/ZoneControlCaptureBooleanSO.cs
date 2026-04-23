using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureBoolean", order = 434)]
    public sealed class ZoneControlCaptureBooleanSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _complementsNeeded    = 6;
        [SerializeField, Min(1)] private int _flipPerBot           = 2;
        [SerializeField, Min(0)] private int _bonusPerComplement   = 3250;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onComplemented;

        private int _complements;
        private int _complementCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ComplementsNeeded   => _complementsNeeded;
        public int   FlipPerBot          => _flipPerBot;
        public int   BonusPerComplement  => _bonusPerComplement;
        public int   Complements         => _complements;
        public int   ComplementCount     => _complementCount;
        public int   TotalBonusAwarded   => _totalBonusAwarded;
        public float ComplementProgress  => _complementsNeeded > 0
            ? Mathf.Clamp01(_complements / (float)_complementsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _complements = Mathf.Min(_complements + 1, _complementsNeeded);
            if (_complements >= _complementsNeeded)
            {
                int bonus = _bonusPerComplement;
                _complementCount++;
                _totalBonusAwarded += bonus;
                _complements        = 0;
                _onComplemented?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _complements = Mathf.Max(0, _complements - _flipPerBot);
        }

        public void Reset()
        {
            _complements       = 0;
            _complementCount   = 0;
            _totalBonusAwarded = 0;
        }
    }
}

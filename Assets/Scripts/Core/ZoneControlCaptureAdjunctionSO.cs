using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAdjunction", order = 381)]
    public sealed class ZoneControlCaptureAdjunctionSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _adjunctsNeeded = 5;
        [SerializeField, Min(1)] private int _splitPerBot    = 1;
        [SerializeField, Min(0)] private int _bonusPerAdjoin = 2455;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAdjunctionAdjoined;

        private int _adjuncts;
        private int _adjoinCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   AdjunctsNeeded    => _adjunctsNeeded;
        public int   SplitPerBot       => _splitPerBot;
        public int   BonusPerAdjoin    => _bonusPerAdjoin;
        public int   Adjuncts          => _adjuncts;
        public int   AdjoinCount       => _adjoinCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float AdjunctProgress   => _adjunctsNeeded > 0
            ? Mathf.Clamp01(_adjuncts / (float)_adjunctsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _adjuncts = Mathf.Min(_adjuncts + 1, _adjunctsNeeded);
            if (_adjuncts >= _adjunctsNeeded)
            {
                int bonus = _bonusPerAdjoin;
                _adjoinCount++;
                _totalBonusAwarded += bonus;
                _adjuncts           = 0;
                _onAdjunctionAdjoined?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _adjuncts = Mathf.Max(0, _adjuncts - _splitPerBot);
        }

        public void Reset()
        {
            _adjuncts          = 0;
            _adjoinCount       = 0;
            _totalBonusAwarded = 0;
        }
    }
}

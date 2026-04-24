using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureResidual", order = 435)]
    public sealed class ZoneControlCaptureResidualSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _residualsNeeded   = 5;
        [SerializeField, Min(1)] private int _annihilatePerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerResiduate = 3265;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onResiduated;

        private int _residuals;
        private int _residuateCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ResidualsNeeded    => _residualsNeeded;
        public int   AnnihilatePerBot   => _annihilatePerBot;
        public int   BonusPerResiduate  => _bonusPerResiduate;
        public int   Residuals          => _residuals;
        public int   ResiduateCount     => _residuateCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ResiduateProgress  => _residualsNeeded > 0
            ? Mathf.Clamp01(_residuals / (float)_residualsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _residuals = Mathf.Min(_residuals + 1, _residualsNeeded);
            if (_residuals >= _residualsNeeded)
            {
                int bonus = _bonusPerResiduate;
                _residuateCount++;
                _totalBonusAwarded += bonus;
                _residuals          = 0;
                _onResiduated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _residuals = Mathf.Max(0, _residuals - _annihilatePerBot);
        }

        public void Reset()
        {
            _residuals         = 0;
            _residuateCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}

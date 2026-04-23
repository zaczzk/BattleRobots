using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureContravariant", order = 378)]
    public sealed class ZoneControlCaptureContravariantSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _contrasNeeded     = 7;
        [SerializeField, Min(1)] private int _reversePerBot     = 2;
        [SerializeField, Min(0)] private int _bonusPerContramap = 2410;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onContramapped;

        private int _contras;
        private int _contramapCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   ContrasNeeded      => _contrasNeeded;
        public int   ReversePerBot      => _reversePerBot;
        public int   BonusPerContramap  => _bonusPerContramap;
        public int   Contras            => _contras;
        public int   ContramapCount     => _contramapCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float ContraProgress     => _contrasNeeded > 0
            ? Mathf.Clamp01(_contras / (float)_contrasNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _contras = Mathf.Min(_contras + 1, _contrasNeeded);
            if (_contras >= _contrasNeeded)
            {
                int bonus = _bonusPerContramap;
                _contramapCount++;
                _totalBonusAwarded += bonus;
                _contras            = 0;
                _onContramapped?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _contras = Mathf.Max(0, _contras - _reversePerBot);
        }

        public void Reset()
        {
            _contras           = 0;
            _contramapCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureIwasawaTheory", order = 513)]
    public sealed class ZoneControlCaptureIwasawaTheorySO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _padicFunctionsNeeded      = 7;
        [SerializeField, Min(1)] private int _selmerObstructionsPerBot   = 2;
        [SerializeField, Min(0)] private int _bonusPerInterpolation      = 4435;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onIwasawaTheoryInterpolated;

        private int _padicFunctions;
        private int _interpolationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PadicFunctionsNeeded     => _padicFunctionsNeeded;
        public int   SelmerObstructionsPerBot  => _selmerObstructionsPerBot;
        public int   BonusPerInterpolation     => _bonusPerInterpolation;
        public int   PadicFunctions            => _padicFunctions;
        public int   InterpolationCount        => _interpolationCount;
        public int   TotalBonusAwarded         => _totalBonusAwarded;
        public float PadicFunctionProgress => _padicFunctionsNeeded > 0
            ? Mathf.Clamp01(_padicFunctions / (float)_padicFunctionsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _padicFunctions = Mathf.Min(_padicFunctions + 1, _padicFunctionsNeeded);
            if (_padicFunctions >= _padicFunctionsNeeded)
            {
                int bonus = _bonusPerInterpolation;
                _interpolationCount++;
                _totalBonusAwarded += bonus;
                _padicFunctions     = 0;
                _onIwasawaTheoryInterpolated?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _padicFunctions = Mathf.Max(0, _padicFunctions - _selmerObstructionsPerBot);
        }

        public void Reset()
        {
            _padicFunctions     = 0;
            _interpolationCount = 0;
            _totalBonusAwarded  = 0;
        }
    }
}

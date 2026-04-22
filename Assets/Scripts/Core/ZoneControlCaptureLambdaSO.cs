using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLambda", order = 363)]
    public sealed class ZoneControlCaptureLambdaSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _lambdasNeeded     = 5;
        [SerializeField, Min(1)] private int _removePerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerExecution = 2185;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLambdaExecuted;

        private int _lambdas;
        private int _executionCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   LambdasNeeded      => _lambdasNeeded;
        public int   RemovePerBot       => _removePerBot;
        public int   BonusPerExecution  => _bonusPerExecution;
        public int   Lambdas            => _lambdas;
        public int   ExecutionCount     => _executionCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float LambdaProgress     => _lambdasNeeded > 0
            ? Mathf.Clamp01(_lambdas / (float)_lambdasNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _lambdas = Mathf.Min(_lambdas + 1, _lambdasNeeded);
            if (_lambdas >= _lambdasNeeded)
            {
                int bonus = _bonusPerExecution;
                _executionCount++;
                _totalBonusAwarded += bonus;
                _lambdas            = 0;
                _onLambdaExecuted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _lambdas = Mathf.Max(0, _lambdas - _removePerBot);
        }

        public void Reset()
        {
            _lambdas           = 0;
            _executionCount    = 0;
            _totalBonusAwarded = 0;
        }
    }
}

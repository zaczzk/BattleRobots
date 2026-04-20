using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMonarch", order = 223)]
    public sealed class ZoneControlCaptureMonarchSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)] private int _capturesForThrone = 4;
        [SerializeField, Min(0)] private int _bonusPerTurn      = 60;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onThroneTaken;
        [SerializeField] private VoidGameEvent _onThroneToppled;

        private bool _isOnThrone;
        private int  _buildCount;
        private int  _throneCount;
        private int  _turnCount;
        private int  _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int  CapturesForThrone => _capturesForThrone;
        public int  BonusPerTurn      => _bonusPerTurn;
        public bool IsOnThrone        => _isOnThrone;
        public int  BuildCount        => _buildCount;
        public int  ThroneCount       => _throneCount;
        public int  TurnCount         => _turnCount;
        public int  TotalBonusAwarded => _totalBonusAwarded;
        public float BuildProgress    => _isOnThrone ? 1f
            : (_capturesForThrone > 0 ? Mathf.Clamp01(_buildCount / (float)_capturesForThrone) : 0f);

        public int RecordPlayerCapture()
        {
            if (_isOnThrone)
            {
                _turnCount++;
                _totalBonusAwarded += _bonusPerTurn;
                return _bonusPerTurn;
            }

            _buildCount++;
            if (_buildCount >= _capturesForThrone)
                TakeThrone();

            return 0;
        }

        public void RecordBotCapture()
        {
            if (_isOnThrone)
                Topple();
            else
                _buildCount = Mathf.Max(0, _buildCount - 1);
        }

        private void TakeThrone()
        {
            _isOnThrone = true;
            _buildCount = 0;
            _throneCount++;
            _onThroneTaken?.Raise();
        }

        private void Topple()
        {
            _isOnThrone = false;
            _onThroneToppled?.Raise();
        }

        public void Reset()
        {
            _isOnThrone        = false;
            _buildCount        = 0;
            _throneCount       = 0;
            _turnCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

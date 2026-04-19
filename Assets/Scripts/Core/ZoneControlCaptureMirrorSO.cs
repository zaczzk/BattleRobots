using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMirror", order = 188)]
    public sealed class ZoneControlCaptureMirrorSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _bonusPerMirror = 175;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMirrorHit;

        private int  _playerCaptures;
        private int  _botCaptures;
        private int  _mirrorCount;
        private int  _totalBonusAwarded;
        private bool _lastWasTied;

        private void OnEnable() => Reset();

        public int  BonusPerMirror    => _bonusPerMirror;
        public int  PlayerCaptures    => _playerCaptures;
        public int  BotCaptures       => _botCaptures;
        public int  MirrorCount       => _mirrorCount;
        public int  TotalBonusAwarded => _totalBonusAwarded;
        public bool IsTied            => _playerCaptures > 0 && _playerCaptures == _botCaptures;

        public void RecordPlayerCapture()
        {
            _playerCaptures++;
            EvaluateMirror();
        }

        public void RecordBotCapture()
        {
            _botCaptures++;
            EvaluateMirror();
        }

        private void EvaluateMirror()
        {
            bool tied = IsTied;
            if (tied && !_lastWasTied)
            {
                _mirrorCount++;
                _totalBonusAwarded += _bonusPerMirror;
                _onMirrorHit?.Raise();
            }
            _lastWasTied = tied;
        }

        public void Reset()
        {
            _playerCaptures    = 0;
            _botCaptures       = 0;
            _mirrorCount       = 0;
            _totalBonusAwarded = 0;
            _lastWasTied       = false;
        }
    }
}

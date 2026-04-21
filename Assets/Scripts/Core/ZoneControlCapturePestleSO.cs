using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCapturePestle", order = 290)]
    public sealed class ZoneControlCapturePestleSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _poundsNeeded    = 5;
        [SerializeField, Min(1)] private int _scatterPerBot   = 1;
        [SerializeField, Min(0)] private int _bonusPerBatch   = 1090;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onPestleProcessed;

        private int _pounds;
        private int _batchCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   PoundsNeeded      => _poundsNeeded;
        public int   ScatterPerBot     => _scatterPerBot;
        public int   BonusPerBatch     => _bonusPerBatch;
        public int   Pounds            => _pounds;
        public int   BatchCount        => _batchCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float PoundProgress     => _poundsNeeded > 0
            ? Mathf.Clamp01(_pounds / (float)_poundsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _pounds = Mathf.Min(_pounds + 1, _poundsNeeded);
            if (_pounds >= _poundsNeeded)
            {
                int bonus = _bonusPerBatch;
                _batchCount++;
                _totalBonusAwarded += bonus;
                _pounds             = 0;
                _onPestleProcessed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _pounds = Mathf.Max(0, _pounds - _scatterPerBot);
        }

        public void Reset()
        {
            _pounds            = 0;
            _batchCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

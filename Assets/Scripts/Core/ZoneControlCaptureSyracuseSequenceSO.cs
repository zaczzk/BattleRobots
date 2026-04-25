using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureSyracuseSequence", order = 534)]
    public sealed class ZoneControlCaptureSyracuseSequenceSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _descentsNeeded      = 6;
        [SerializeField, Min(1)] private int _ascentSpikesPerBot  = 1;
        [SerializeField, Min(0)] private int _bonusPerDescent     = 4750;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onSyracuseSequenceDescended;

        private int _descents;
        private int _descentCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   DescentsNeeded         => _descentsNeeded;
        public int   AscentSpikesPerBot     => _ascentSpikesPerBot;
        public int   BonusPerDescent        => _bonusPerDescent;
        public int   Descents               => _descents;
        public int   DescentCount           => _descentCount;
        public int   TotalBonusAwarded      => _totalBonusAwarded;
        public float DescentProgress        => _descentsNeeded > 0
            ? Mathf.Clamp01(_descents / (float)_descentsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _descents = Mathf.Min(_descents + 1, _descentsNeeded);
            if (_descents >= _descentsNeeded)
            {
                int bonus = _bonusPerDescent;
                _descentCount++;
                _totalBonusAwarded += bonus;
                _descents           = 0;
                _onSyracuseSequenceDescended?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _descents = Mathf.Max(0, _descents - _ascentSpikesPerBot);
        }

        public void Reset()
        {
            _descents          = 0;
            _descentCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

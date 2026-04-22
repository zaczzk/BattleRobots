using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureAmplifier", order = 325)]
    public sealed class ZoneControlCaptureAmplifierSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _stagesNeeded          = 7;
        [SerializeField, Min(1)] private int _attenuatePerBot       = 2;
        [SerializeField, Min(0)] private int _bonusPerAmplification = 1615;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onAmplifierBoosted;

        private int _stages;
        private int _amplificationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   StagesNeeded          => _stagesNeeded;
        public int   AttenuatePerBot       => _attenuatePerBot;
        public int   BonusPerAmplification => _bonusPerAmplification;
        public int   Stages                => _stages;
        public int   AmplificationCount    => _amplificationCount;
        public int   TotalBonusAwarded     => _totalBonusAwarded;
        public float StageProgress         => _stagesNeeded > 0
            ? Mathf.Clamp01(_stages / (float)_stagesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _stages = Mathf.Min(_stages + 1, _stagesNeeded);
            if (_stages >= _stagesNeeded)
            {
                int bonus = _bonusPerAmplification;
                _amplificationCount++;
                _totalBonusAwarded += bonus;
                _stages             = 0;
                _onAmplifierBoosted?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _stages = Mathf.Max(0, _stages - _attenuatePerBot);
        }

        public void Reset()
        {
            _stages             = 0;
            _amplificationCount = 0;
            _totalBonusAwarded  = 0;
        }
    }
}

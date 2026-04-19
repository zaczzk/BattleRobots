using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureNexus", order = 189)]
    public sealed class ZoneControlCaptureNexusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _bonusPerNexus = 300;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNexusComplete;

        // Step 0 = idle, 1 = awaiting bot, 2 = awaiting player to close
        private int _nexusStep;
        private int _nexusCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   BonusPerNexus     => _bonusPerNexus;
        public int   NexusStep         => _nexusStep;
        public int   NexusCount        => _nexusCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float NexusProgress     => _nexusStep / 3f;

        public void RecordPlayerCapture()
        {
            if (_nexusStep == 0)
            {
                _nexusStep = 1;
            }
            else if (_nexusStep == 2)
            {
                _nexusStep          = 0;
                _nexusCount++;
                _totalBonusAwarded += _bonusPerNexus;
                _onNexusComplete?.Raise();
            }
        }

        public void RecordBotCapture()
        {
            if (_nexusStep == 1)
                _nexusStep = 2;
            else
                _nexusStep = 0;
        }

        public void Reset()
        {
            _nexusStep         = 0;
            _nexusCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

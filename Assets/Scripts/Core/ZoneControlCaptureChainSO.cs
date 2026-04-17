using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that detects a consecutive unique-zone capture chain.
    ///
    /// Call <see cref="AddCapture(int)"/> with the captured zone index each time the
    /// player captures a zone.  Capturing the same zone as last time resets the chain.
    /// When <see cref="CurrentChainLength"/> reaches <see cref="ChainTarget"/> the chain
    /// completes: <c>_onChainCompleted</c> fires, <see cref="CompletedChains"/> increments,
    /// and the chain length resets.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlCaptureChain.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureChain", order = 90)]
    public sealed class ZoneControlCaptureChainSO : ScriptableObject
    {
        [Header("Chain Settings")]
        [Min(2)]
        [SerializeField] private int _chainTarget = 3;

        [Min(0)]
        [SerializeField] private int _chainBonus = 125;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onChainCompleted;

        private int _lastZoneIndex     = -1;
        private int _currentChainLength;
        private int _completedChains;

        private void OnEnable() => Reset();

        public int ChainTarget         => _chainTarget;
        public int ChainBonus          => _chainBonus;
        public int CurrentChainLength  => _currentChainLength;
        public int CompletedChains     => _completedChains;

        /// <summary>
        /// Records a capture at the given zone index.  Capturing the same zone as the
        /// last capture resets the chain.  Reaching <see cref="ChainTarget"/> fires
        /// <c>_onChainCompleted</c>, increments <see cref="CompletedChains"/>, and
        /// resets the chain length.
        /// </summary>
        public void AddCapture(int zoneIndex)
        {
            if (zoneIndex == _lastZoneIndex)
            {
                _currentChainLength = 1;
                _lastZoneIndex      = zoneIndex;
                return;
            }

            _lastZoneIndex = zoneIndex;
            _currentChainLength++;

            if (_currentChainLength >= _chainTarget)
            {
                _completedChains++;
                _currentChainLength = 0;
                _lastZoneIndex      = -1;
                _onChainCompleted?.Raise();
            }
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _lastZoneIndex      = -1;
            _currentChainLength = 0;
            _completedChains    = 0;
        }
    }
}

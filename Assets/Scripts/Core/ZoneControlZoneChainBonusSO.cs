using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Awards a bonus when the player captures <c>_chainTarget</c> consecutive zones
    /// without any bot recapturing a zone in between.
    /// <c>RecordPlayerCapture()</c> advances the chain; <c>RecordBotCapture()</c> resets it.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneChainBonus.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneChainBonus", order = 127)]
    public sealed class ZoneControlZoneChainBonusSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(2)] private int _chainTarget    = 3;
        [SerializeField, Min(0)] private int _bonusPerChain  = 150;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onChainCompleted;
        [SerializeField] private VoidGameEvent _onChainBroken;

        private int _chainLength;
        private int _chainCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int ChainTarget       => _chainTarget;
        public int BonusPerChain     => _bonusPerChain;
        public int ChainLength       => _chainLength;
        public int ChainCount        => _chainCount;
        public int TotalBonusAwarded => _totalBonusAwarded;

        /// <summary>Progress toward next chain completion [0,1].</summary>
        public float ChainProgress => Mathf.Clamp01((float)_chainLength / Mathf.Max(1, _chainTarget));

        public void RecordPlayerCapture()
        {
            _chainLength++;
            if (_chainLength >= _chainTarget)
            {
                _chainCount++;
                _totalBonusAwarded += _bonusPerChain;
                _chainLength = 0;
                _onChainCompleted?.Raise();
            }
        }

        public void RecordBotCapture()
        {
            if (_chainLength > 0)
            {
                _chainLength = 0;
                _onChainBroken?.Raise();
            }
        }

        public void Reset()
        {
            _chainLength       = 0;
            _chainCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks "score momentum" — the time the player holds a
    /// zone-count lead — and rewards periodic bonuses while leading.
    ///
    /// Call <see cref="SetLeading"/> with <c>true</c> when the player gains
    /// majority zone control and <c>false</c> when they lose it.
    /// Drive <see cref="Tick"/> from a MonoBehaviour Update loop.
    /// Fires <c>_onMomentumGained/_onMomentumLost</c> on lead transitions and
    /// <c>_onMomentumBonus</c> for each completed leading interval.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlScoreMomentum.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlScoreMomentum", order = 101)]
    public sealed class ZoneControlScoreMomentumSO : ScriptableObject
    {
        [Header("Momentum Settings")]
        [Min(1f)]
        [SerializeField] private float _bonusInterval = 10f;

        [Min(0)]
        [SerializeField] private int _bonusPerInterval = 50;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMomentumGained;
        [SerializeField] private VoidGameEvent _onMomentumLost;
        [SerializeField] private VoidGameEvent _onMomentumBonus;

        private bool  _isLeading;
        private float _elapsedWhileLeading;
        private int   _intervalsCompleted;
        private int   _totalBonusAwarded;
        private float _nextMilestone;

        private void OnEnable() => Reset();

        public bool  IsLeading            => _isLeading;
        public float ElapsedWhileLeading  => _elapsedWhileLeading;
        public int   IntervalsCompleted   => _intervalsCompleted;
        public int   TotalBonusAwarded    => _totalBonusAwarded;
        public float BonusInterval        => _bonusInterval;
        public int   BonusPerInterval     => _bonusPerInterval;

        /// <summary>
        /// Sets the player's lead state.  Fires <c>_onMomentumGained</c> on
        /// false→true and <c>_onMomentumLost</c> on true→false.  Idempotent.
        /// </summary>
        public void SetLeading(bool leading)
        {
            if (leading == _isLeading) return;
            _isLeading = leading;
            if (_isLeading)
                _onMomentumGained?.Raise();
            else
                _onMomentumLost?.Raise();
        }

        /// <summary>
        /// Advances the leading timer by <paramref name="dt"/> seconds.
        /// No-op when not leading.  Fires <c>_onMomentumBonus</c> for each
        /// completed interval (multi-interval safe).
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isLeading) return;
            _elapsedWhileLeading += dt;
            while (_elapsedWhileLeading >= _nextMilestone)
            {
                _intervalsCompleted++;
                _totalBonusAwarded += _bonusPerInterval;
                _nextMilestone     += _bonusInterval;
                _onMomentumBonus?.Raise();
            }
        }

        /// <summary>Clears all runtime state silently.</summary>
        public void Reset()
        {
            _isLeading           = false;
            _elapsedWhileLeading = 0f;
            _intervalsCompleted  = 0;
            _totalBonusAwarded   = 0;
            _nextMilestone       = _bonusInterval;
        }
    }
}

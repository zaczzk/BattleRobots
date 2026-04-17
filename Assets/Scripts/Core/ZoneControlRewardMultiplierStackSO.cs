using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that maintains a time-decaying stack of reward multipliers.
    /// Each zone capture adds one stack (clamped to <c>_maxStacks</c>); stacks decay
    /// one-by-one on a fixed interval.
    ///
    /// <c>CurrentMultiplier = 1 + CurrentStacks × _multiplierPerStack</c>.
    /// <c>ComputeBonus(amount)</c> returns <c>RoundToInt(amount × CurrentMultiplier)</c>.
    /// Fires <c>_onStacksChanged</c> whenever the stack count changes.
    /// <see cref="Reset"/> clears all state silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlRewardMultiplierStack.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlRewardMultiplierStack", order = 93)]
    public sealed class ZoneControlRewardMultiplierStackSO : ScriptableObject
    {
        [Header("Stack Settings")]
        [Min(1)]
        [SerializeField] private int _maxStacks = 5;

        [Min(0.01f)]
        [SerializeField] private float _multiplierPerStack = 0.1f;

        [Min(0.1f)]
        [SerializeField] private float _stackDecayInterval = 2f;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onStacksChanged;

        private int   _stacks;
        private float _decayAccumulator;

        private void OnEnable() => Reset();

        public int   MaxStacks          => _maxStacks;
        public int   CurrentStacks      => _stacks;
        public float MultiplierPerStack => _multiplierPerStack;
        public float StackDecayInterval => _stackDecayInterval;
        public float CurrentMultiplier  => 1f + _stacks * _multiplierPerStack;

        /// <summary>Adds one stack (clamped to <see cref="MaxStacks"/>) and fires the change event.</summary>
        public void AddStack()
        {
            _stacks = Mathf.Min(_stacks + 1, _maxStacks);
            _onStacksChanged?.Raise();
        }

        /// <summary>
        /// Advances the decay accumulator by <paramref name="dt"/>; removes one stack
        /// per completed <see cref="StackDecayInterval"/> while stacks remain.
        /// Fires <c>_onStacksChanged</c> on each decrement.
        /// </summary>
        public void Tick(float dt)
        {
            if (_stacks <= 0) return;

            _decayAccumulator += dt;
            while (_decayAccumulator >= _stackDecayInterval && _stacks > 0)
            {
                _stacks--;
                _decayAccumulator -= _stackDecayInterval;
                _onStacksChanged?.Raise();
            }

            if (_stacks <= 0)
                _decayAccumulator = 0f;
        }

        /// <summary>Scales <paramref name="amount"/> by <see cref="CurrentMultiplier"/> and rounds to int.</summary>
        public int ComputeBonus(int amount)
        {
            return Mathf.RoundToInt(amount * CurrentMultiplier);
        }

        /// <summary>Clears stacks and resets the decay accumulator silently.</summary>
        public void Reset()
        {
            _stacks           = 0;
            _decayAccumulator = 0f;
        }
    }
}

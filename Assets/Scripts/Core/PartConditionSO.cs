using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime blackboard SO that tracks the structural condition (HP) of a single robot part.
    ///
    /// ── Purpose ────────────────────────────────────────────────────────────────
    ///   Each physically equipped part (weapon, chassis panel, leg assembly, etc.) owns
    ///   one PartConditionSO. Incoming damage is distributed to individual parts by
    ///   <see cref="BattleRobots.Physics.PartHealthSystem"/>, giving parts independent
    ///   destruction state without coupling Physics to UI or Core SO channels.
    ///
    /// ── Lifecycle ──────────────────────────────────────────────────────────────
    ///   OnEnable  — resets CurrentHP to MaxHP so the SO is always clean at play start.
    ///   TakeDamage — reduces CurrentHP; fires _onPartDestroyed once when HP first hits 0.
    ///   Repair     — restores HP; can revive a destroyed part (re-entry from power-ups).
    ///   ResetToMax — fully restores HP and clears destroyed flag without firing events.
    ///               Call at match start (or use OnEnable — whichever comes first).
    ///
    /// ── Architecture notes ─────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on hot path: TakeDamage/Repair are float arithmetic only.
    ///   - SO assets are write-protected at runtime — only TakeDamage(), Repair(),
    ///     and ResetToMax() may mutate internal state.
    ///   - _onPartDestroyed fires at most once per lifetime (until next ResetToMax/Repair).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ PartCondition.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/PartCondition")]
    public sealed class PartConditionSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Part Stats")]
        [Tooltip("Maximum HP this part can sustain. Minimum 1 to avoid division-by-zero.")]
        [SerializeField, Min(1f)] private float _maxHP = 50f;

        [Header("Event Channel (optional)")]
        [Tooltip("Raised once when CurrentHP first reaches zero (part destroyed). " +
                 "Wire to VFX, audio, or RobotStatsAggregator to react to part loss. " +
                 "Leave null to skip (backwards-compatible).")]
        [SerializeField] private VoidGameEvent _onPartDestroyed;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float _currentHP;
        private bool  _destroyed;

        // ── Unity callbacks ───────────────────────────────────────────────────

        private void OnEnable()
        {
            // Called when the asset is loaded (editor startup / entering Play mode).
            // Ensures every play session starts with a full part, regardless of prior state.
            _currentHP = _maxHP;
            _destroyed = false;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Maximum HP configured in the Inspector.</summary>
        public float MaxHP => _maxHP;

        /// <summary>Remaining HP. In [0, MaxHP]. Zero means the part is destroyed.</summary>
        public float CurrentHP => _currentHP;

        /// <summary>True once CurrentHP reaches zero. Stays true until Repair or ResetToMax.</summary>
        public bool IsDestroyed => _destroyed;

        /// <summary>CurrentHP / MaxHP in [0, 1]. Zero when destroyed or uninitialized.</summary>
        public float HPRatio => _maxHP > 0f ? Mathf.Clamp01(_currentHP / _maxHP) : 0f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reduces the part's HP by <paramref name="amount"/>.
        /// Clamps to 0. Fires <see cref="_onPartDestroyed"/> exactly once when the part
        /// transitions from alive → destroyed. No-ops on already-destroyed parts or
        /// non-positive amounts. Zero allocation.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (_destroyed || amount <= 0f) return;

            _currentHP = Mathf.Max(0f, _currentHP - amount);

            if (_currentHP <= 0f && !_destroyed)
            {
                _destroyed = true;
                _onPartDestroyed?.Raise();
            }
        }

        /// <summary>
        /// Restores HP by <paramref name="amount"/>, clamped to MaxHP.
        /// If the part was destroyed, Repair revives it (IsDestroyed becomes false
        /// once HP is above zero again). Ignores non-positive amounts. Zero allocation.
        /// </summary>
        public void Repair(float amount)
        {
            if (amount <= 0f) return;

            _currentHP = Mathf.Min(_maxHP, _currentHP + amount);
            // Revive: HP above zero means no longer destroyed.
            if (_currentHP > 0f)
                _destroyed = false;
        }

        /// <summary>
        /// Instantly restores this part to full HP and clears the destroyed flag.
        /// Fires no events. Call at match start or on robot respawn.
        /// </summary>
        public void ResetToMax()
        {
            _currentHP = _maxHP;
            _destroyed = false;
        }
    }
}

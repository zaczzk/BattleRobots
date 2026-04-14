using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks cooldown state for a single weapon slot.
    ///
    /// ── Lifecycle ────────────────────────────────────────────────────────────────
    ///   1. Call <see cref="StartCooldown"/> when the weapon fires.
    ///   2. Call <see cref="Tick"/> each frame from a driving MonoBehaviour's Update.
    ///   3. When <see cref="IsOnCooldown"/> becomes false the weapon is ready again.
    ///
    /// ── Events ──────────────────────────────────────────────────────────────────
    ///   <see cref="_onCooldownChanged"/> — VoidGameEvent raised on StartCooldown and
    ///     each Tick while the cooldown is active; wire to HUD controllers.
    ///   <see cref="_onCooldownComplete"/> — VoidGameEvent raised once when remaining
    ///     cooldown reaches zero; use for a "ready" audio / VFX cue.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on hot-path Tick() (float arithmetic only).
    ///   - SO assets are immutable at runtime — only cooldown-state fields mutate.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ WeaponCooldown.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/WeaponCooldown")]
    public sealed class WeaponCooldownSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Cooldown Settings")]
        [Tooltip("Duration of the cooldown after the weapon fires, in seconds.")]
        [SerializeField, Min(0f)] private float _maxCooldown = 2f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised on StartCooldown and each Tick while the cooldown is active.")]
        [SerializeField] private VoidGameEvent _onCooldownChanged;

        [Tooltip("Raised once when the cooldown reaches zero.")]
        [SerializeField] private VoidGameEvent _onCooldownComplete;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private float _remainingCooldown;
        private bool  _completed;   // edge-guard for _onCooldownComplete

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Maximum cooldown duration configured on this asset.</summary>
        public float MaxCooldown => _maxCooldown;

        /// <summary>Seconds remaining until the weapon is ready again. Always ≥ 0.</summary>
        public float RemainingCooldown => _remainingCooldown;

        /// <summary>
        /// Normalised cooldown ratio in [0, 1] (1 = just fired; 0 = ready).
        /// Suitable for driving a fill bar directly.
        /// </summary>
        public float CooldownRatio =>
            _maxCooldown > 0f ? Mathf.Clamp01(_remainingCooldown / _maxCooldown) : 0f;

        /// <summary>True while the weapon is cooling down.</summary>
        public bool IsOnCooldown => _remainingCooldown > 0f;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _remainingCooldown = 0f;
            _completed         = true;   // Start in "ready" state; no spurious complete event.
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Begins the cooldown by setting remaining to <see cref="MaxCooldown"/>.
        /// Fires <see cref="_onCooldownChanged"/>.
        /// </summary>
        public void StartCooldown()
        {
            _remainingCooldown = _maxCooldown;
            _completed         = false;
            _onCooldownChanged?.Raise();
        }

        /// <summary>
        /// Advances the cooldown by <paramref name="deltaTime"/> seconds.
        /// No-op when not on cooldown.
        /// Fires <see cref="_onCooldownChanged"/> while decrementing.
        /// Fires <see cref="_onCooldownComplete"/> once when reaching zero.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsOnCooldown) return;

            _remainingCooldown = Mathf.Max(0f, _remainingCooldown - deltaTime);
            _onCooldownChanged?.Raise();

            if (_remainingCooldown <= 0f && !_completed)
            {
                _completed = true;
                _onCooldownComplete?.Raise();
            }
        }
    }
}

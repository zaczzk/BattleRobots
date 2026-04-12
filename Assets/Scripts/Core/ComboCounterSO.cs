using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime blackboard SO that tracks the player's hit combo streak during a match.
    ///
    /// ── Combo rules ────────────────────────────────────────────────────────────
    ///   • Each successful player hit calls <see cref="RecordHit"/>, which increments
    ///     the current <see cref="HitCount"/> and resets the combo window timer.
    ///   • If no hit is registered within <see cref="ComboWindowSeconds"/> seconds,
    ///     <see cref="Tick"/> breaks the combo: HitCount resets to 0 and
    ///     <see cref="_onComboBreak"/> fires.
    ///   • <see cref="ComboMultiplier"/> = 1.0 + floor(HitCount / 5) × 0.1, capped at 2.0.
    ///     (Every 5 consecutive hits adds +0.1× to the score multiplier.)
    ///   • <see cref="MaxCombo"/> tracks the highest streak reached this match session.
    ///
    /// ── Integration ────────────────────────────────────────────────────────────
    ///   • Call <see cref="RecordHit"/> from a DamageGameEventListener response or
    ///     directly from a projectile/weapon script after a confirmed hit.
    ///   • <see cref="Tick"/> must be driven each frame by <c>ComboHUDController.Update()</c>
    ///     (zero allocation — no MonoBehaviour needed on this SO).
    ///   • Call <see cref="Reset"/> at match start (wire via VoidGameEventListener
    ///     MatchStarted → ComboCounterSO.Reset).
    ///   • Wire <see cref="_onComboChanged"/> → ComboHUDController subscription so the
    ///     HUD refreshes text on each hit or break without polling.
    ///
    /// ── Architecture notes ─────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Zero heap allocation on all hot-path methods (value-type arithmetic only).
    ///   - SO assets are immutable at runtime — only RecordHit(), Tick(), and Reset()
    ///     may mutate internal state.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Combat ▶ ComboCounter.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Combat/ComboCounter")]
    public sealed class ComboCounterSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Combo Window")]
        [Tooltip("Seconds after the last hit before the combo breaks. " +
                 "Recommended: 2–4 seconds to allow for weapon cooldowns.")]
        [SerializeField, Min(0.1f)] private float _comboWindowSeconds = 3f;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised on every RecordHit() call and also when the combo breaks. " +
                 "Wire to ComboHUDController to refresh text/icons reactively.")]
        [SerializeField] private VoidGameEvent _onComboChanged;

        [Tooltip("Raised specifically when the combo timer expires (streak broken). " +
                 "Use for audio feedback (e.g. 'combo lost' sound).")]
        [SerializeField] private VoidGameEvent _onComboBreak;

        [Tooltip("Raised when a new all-time-high MaxCombo is achieved this session. " +
                 "Use for a celebratory sound or screen flash.")]
        [SerializeField] private VoidGameEvent _onNewMaxCombo;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private int   _hitCount;
        private int   _maxCombo;
        private float _comboMultiplier = 1f;
        private float _comboTimer;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Current number of consecutive hits in the active streak.</summary>
        public int HitCount => _hitCount;

        /// <summary>Highest combo streak reached since the last <see cref="Reset"/>.</summary>
        public int MaxCombo => _maxCombo;

        /// <summary>
        /// Score multiplier based on the current streak.
        /// Formula: 1.0 + floor(HitCount / 5) × 0.1, capped at 2.0.
        ///   5 hits → 1.1×   10 hits → 1.2×   50 hits → 2.0× (cap).
        /// Returns 1.0 when no combo is active.
        /// </summary>
        public float ComboMultiplier => _comboMultiplier;

        /// <summary>True when at least one hit has been recorded and the window is open.</summary>
        public bool IsComboActive => _hitCount > 0;

        /// <summary>Configurable combo sustain window in seconds.</summary>
        public float ComboWindowSeconds => _comboWindowSeconds;

        /// <summary>
        /// Normalised ratio of remaining combo window in [0, 1]:
        ///   1 = full window remaining (just hit),  0 = expired / inactive.
        /// Suitable for driving an Image.fillAmount countdown bar.
        /// Zero when no combo is active.
        /// </summary>
        public float ComboWindowRatio =>
            _hitCount > 0 && _comboWindowSeconds > 0f
                ? Mathf.Clamp01(_comboTimer / _comboWindowSeconds)
                : 0f;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Records one successful hit, extending (or starting) the combo streak.
        /// Resets the combo window timer, recalculates the multiplier,
        /// updates MaxCombo if a new high is reached, then fires
        /// <see cref="_onNewMaxCombo"/> (if beaten) and <see cref="_onComboChanged"/>.
        /// Zero allocation — all value-type arithmetic.
        /// </summary>
        public void RecordHit()
        {
            _hitCount++;
            _comboTimer      = _comboWindowSeconds;
            _comboMultiplier = ComputeMultiplier(_hitCount);

            if (_hitCount > _maxCombo)
            {
                _maxCombo = _hitCount;
                _onNewMaxCombo?.Raise();
            }

            _onComboChanged?.Raise();
        }

        /// <summary>
        /// Advances the combo window timer by <paramref name="deltaTime"/> seconds.
        /// Must be called each frame (e.g. from <c>ComboHUDController.Update()</c>).
        /// When the window expires, automatically breaks the combo:
        /// HitCount resets to 0 and <see cref="_onComboBreak"/> + <see cref="_onComboChanged"/> fire.
        /// No-op when no combo is active (<see cref="IsComboActive"/> == false).
        /// Zero allocation.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_hitCount == 0) return;

            _comboTimer -= deltaTime;
            if (_comboTimer <= 0f)
                BreakCombo();
        }

        /// <summary>
        /// Clears all combo state including MaxCombo.
        /// Call at match start (e.g. via VoidGameEventListener MatchStarted → Reset).
        /// Fires no events.
        /// </summary>
        public void Reset()
        {
            _hitCount        = 0;
            _maxCombo        = 0;
            _comboMultiplier = 1f;
            _comboTimer      = 0f;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void BreakCombo()
        {
            _hitCount        = 0;
            _comboTimer      = 0f;
            _comboMultiplier = 1f;
            _onComboBreak?.Raise();
            _onComboChanged?.Raise();
        }

        /// <summary>
        /// Multiplier tier: 1.0 base + 0.1 per complete set of 5 hits, capped at 2.0.
        /// Pure integer arithmetic — zero allocation.
        /// </summary>
        private static float ComputeMultiplier(int hits) =>
            Mathf.Min(2f, 1f + Mathf.FloorToInt(hits / 5) * 0.1f);
    }
}

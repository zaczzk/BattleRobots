using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Applies a diminishing Perlin-noise positional offset to this Transform
    /// to simulate camera shake on robot death or heavy impacts.
    ///
    /// Designed to sit on the same GameObject as <see cref="CameraRig"/>.
    /// <c>[DefaultExecutionOrder(100)]</c> ensures this LateUpdate runs after CameraRig's
    /// LateUpdate (default order 0), so the shake offset is applied on top of the
    /// already-computed follow position without being overwritten.
    ///
    /// ── Trigger options ──────────────────────────────────────────────────────
    ///   A. Inspector — assign any number of VoidGameEvent SOs to <c>_shakeEvents</c>.
    ///      Each event fires a shake with <c>_defaultMagnitude</c> / <c>_defaultDuration</c>.
    ///   B. Code — call <c>Shake(magnitude, duration)</c> directly.
    ///      If a stronger shake is requested while one is running, it takes over.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.Core namespace — no Physics or UI references.
    ///   • Zero heap allocations in LateUpdate: all maths uses struct types.
    ///   • Callbacks are cached in Awake to prevent per-enable allocation.
    /// </summary>
    [DefaultExecutionOrder(100)]   // run after CameraRig (order 0)
    public sealed class CameraShake : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Shake Events")]
        [Tooltip("VoidGameEvent channels (e.g., player-death, enemy-death) that trigger " +
                 "a shake with the default magnitude and duration below.")]
        [SerializeField] private VoidGameEvent[] _shakeEvents;

        [Header("Default Shake Parameters")]
        [Tooltip("World-space offset magnitude (units) applied at full intensity.")]
        [SerializeField, Min(0f)] private float _defaultMagnitude = 0.3f;

        [Tooltip("Seconds over which the shake decays to zero.")]
        [SerializeField, Min(0f)] private float _defaultDuration = 0.4f;

        [Header("Noise")]
        [Tooltip("Speed at which the Perlin noise scrolls. Higher = more rapid jitter.")]
        [SerializeField, Min(1f)] private float _noiseFrequency = 25f;

        // ── Private state (value types — zero alloc) ──────────────────────────

        private float _shakeMagnitude;
        private float _shakeDuration;
        private float _shakeTimer;

        // Perlin seed offsets — randomised in Awake so multiple CameraShakes differ.
        private float _seedX;
        private float _seedY;

        // Cached delegate — prevents allocation per OnEnable call.
        private System.Action _onShakeEvent;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _seedX       = Random.Range(0f, 100f);
            _seedY       = Random.Range(100f, 200f);
            _onShakeEvent = OnShakeEventRaised;
        }

        private void OnEnable()
        {
            if (_shakeEvents == null) return;
            foreach (VoidGameEvent evt in _shakeEvents)
                evt?.RegisterCallback(_onShakeEvent);
        }

        private void OnDisable()
        {
            if (_shakeEvents == null) return;
            foreach (VoidGameEvent evt in _shakeEvents)
                evt?.UnregisterCallback(_onShakeEvent);
        }

        private void LateUpdate()
        {
            if (_shakeTimer <= 0f) return;

            _shakeTimer -= Time.deltaTime;

            // t: 1 at start → 0 at end (linear decay).
            float t = Mathf.Clamp01(_shakeTimer / _shakeDuration);

            // Perlin noise centred on 0 in both axes — no allocation.
            float time   = Time.time * _noiseFrequency;
            float offsetX = (Mathf.PerlinNoise(time + _seedX, 0f) - 0.5f) * 2f * _shakeMagnitude * t;
            float offsetY = (Mathf.PerlinNoise(0f, time + _seedY) - 0.5f) * 2f * _shakeMagnitude * t;

            // Add offset on top of the position already set by CameraRig.LateUpdate.
            transform.position += new Vector3(offsetX, offsetY, 0f);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Triggers a shake. If a stronger shake is already running it is preserved;
        /// a weaker request replaces only if the running shake has expired.
        /// </summary>
        public void Shake(float magnitude, float duration)
        {
            if (magnitude > _shakeMagnitude || _shakeTimer <= 0f)
            {
                _shakeMagnitude = magnitude;
                _shakeDuration  = duration;
                _shakeTimer     = duration;
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void OnShakeEventRaised()
        {
            Shake(_defaultMagnitude, _defaultDuration);
        }
    }
}

using System.Collections;
using UnityEngine;
using BattleRobots.Core;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Flashes all child <see cref="Renderer"/> components briefly whenever the
    /// robot's <see cref="HealthSO"/> reports a decrease in health (i.e. damage was
    /// taken).  Heals are silently ignored.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Listens to <c>_onHealthChanged</c> FloatGameEvent (same SO asset as
    ///   <c>HealthSO._onHealthChanged</c>).  On each event the payload (current
    ///   health) is compared to the tracked previous health; a decrease triggers
    ///   the flash coroutine.
    ///
    /// ── Rendering ────────────────────────────────────────────────────────────
    ///   Uses <see cref="MaterialPropertyBlock"/> to tint all child Renderers —
    ///   no material instantiation, zero heap allocation in the hot path.
    ///   After the flash duration expires the property block is cleared, restoring
    ///   each Renderer's original material appearance.
    ///
    /// ── Pause safety ─────────────────────────────────────────────────────────
    ///   <see cref="WaitForSecondsRealtime"/> keeps the flash visible even when
    ///   <c>Time.timeScale == 0</c> (game paused).
    ///
    /// ── Architecture rules ────────────────────────────────────────────────────
    ///   • BattleRobots.Physics namespace — may reference ArticulationBody hierarchy.
    ///   • BattleRobots.UI must NOT reference this component.
    ///   • No heap allocation in the health-change callback; only the coroutine
    ///     <c>StartCoroutine</c> call allocates (once per hit — cold path).
    ///   • No Update / FixedUpdate.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Attach to the robot's root GameObject (alongside DamageReceiver).
    ///   2. Assign <c>_health</c>        → the robot's HealthSO asset.
    ///   3. Assign <c>_onHealthChanged</c> → the FloatGameEvent SO wired inside
    ///      that HealthSO (same asset as <c>HealthSO._onHealthChanged</c>).
    ///   4. Optionally assign <c>_config</c> → a DamageFlashConfig SO; if null
    ///      a built-in default (0.15 s, red) is used.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageFlashController : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────

        // Shader property ID cached once — allocation-free on subsequent reads.
        private static readonly int k_ColorPropertyId = Shader.PropertyToID("_Color");

        // Built-in defaults used when _config is null.
        private const float k_DefaultFlashDuration = 0.15f;
        private static readonly Color k_DefaultFlashColor = Color.red;

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Required")]
        [Tooltip("HealthSO asset for this robot. Used to initialise _previousHealth " +
                 "when the component enables so the first health event compares correctly.")]
        [SerializeField] private HealthSO _health;

        [Tooltip("FloatGameEvent SO that HealthSO raises on every health change. " +
                 "Wire the same asset here as HealthSO._onHealthChanged.")]
        [SerializeField] private FloatGameEvent _onHealthChanged;

        [Header("Optional")]
        [Tooltip("Flash configuration (colour + duration). " +
                 "Leave null to use the built-in default: 0.15 s red flash.")]
        [SerializeField] private DamageFlashConfig _config;

        // ── Runtime state ─────────────────────────────────────────────────────

        // All child renderers cached in Awake; no per-frame search.
        private Renderer[] _renderers;

        // Reused across all flash calls — avoids repeated heap allocations.
        private MaterialPropertyBlock _block;

        // Cached delegate so OnEnable/OnDisable never allocate.
        private System.Action<float> _healthChangedDelegate;

        // Tracks the running flash coroutine so it can be interrupted on OnDisable.
        private Coroutine _flashRoutine;

        // Previous health value used to detect damage vs heal events.
        private float _previousHealth;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Awake()
        {
            // Collect all child Renderers once — zero allocation on hot paths.
            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

            // One MaterialPropertyBlock shared across all flash calls.
            _block = new MaterialPropertyBlock();

            // Cache delegate; never re-creates after this point.
            _healthChangedDelegate = HandleHealthChanged;
        }

        private void OnEnable()
        {
            // Snapshot current health so the first HandleHealthChanged comparison is valid.
            _previousHealth = _health != null ? _health.CurrentHealth : 0f;

            _onHealthChanged?.RegisterCallback(_healthChangedDelegate);
        }

        private void OnDisable()
        {
            _onHealthChanged?.UnregisterCallback(_healthChangedDelegate);

            // Stop any running flash and restore renderer colours immediately.
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
                ResetRenderers();
            }
        }

        // ── Event handler ─────────────────────────────────────────────────────

        /// <summary>
        /// Receives the current health from <c>HealthSO._onHealthChanged</c>.
        /// Triggers a flash coroutine only when health has decreased (damage taken).
        /// Heals (health increase) are silently ignored.
        /// No heap allocation — value-type comparison only.
        /// </summary>
        private void HandleHealthChanged(float currentHealth)
        {
            bool damageTaken = currentHealth < _previousHealth;
            _previousHealth = currentHealth;

            if (!damageTaken) return;

            // If a flash is already running, restart it so repeated hits
            // always give the full flash duration.
            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);

            _flashRoutine = StartCoroutine(FlashCoroutine());
        }

        // ── Flash coroutine ────────────────────────────────────────────────────

        private IEnumerator FlashCoroutine()
        {
            float duration = _config != null ? _config.FlashDuration : k_DefaultFlashDuration;
            Color  color   = _config != null ? _config.FlashColor    : k_DefaultFlashColor;

            SetRenderersColor(color);

            // WaitForSecondsRealtime: flash persists even while game is paused.
            yield return new WaitForSecondsRealtime(duration);

            ResetRenderers();
            _flashRoutine = null;
        }

        // ── Renderer helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Applies <paramref name="color"/> to every cached Renderer via a shared
        /// <see cref="MaterialPropertyBlock"/> — no material instantiation.
        /// </summary>
        private void SetRenderersColor(Color color)
        {
            if (_renderers == null) return;

            _block.SetColor(k_ColorPropertyId, color);
            foreach (Renderer r in _renderers)
            {
                if (r != null)
                    r.SetPropertyBlock(_block);
            }
        }

        /// <summary>
        /// Clears the shared property block and reapplies it to every cached Renderer,
        /// restoring their default material appearance.
        /// </summary>
        private void ResetRenderers()
        {
            if (_renderers == null) return;

            _block.Clear();
            foreach (Renderer r in _renderers)
            {
                if (r != null)
                    r.SetPropertyBlock(_block);
            }
        }
    }
}

using System.Collections;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that implements a single power-up pickup within an arena.
    ///
    /// ── Behaviour ────────────────────────────────────────────────────────────────
    ///   1. While active (<see cref="IsActive"/> == true) and a robot with a
    ///      <see cref="DamageReceiver"/> enters the attached trigger collider,
    ///      <see cref="OnTriggerEnter"/> fires the pickup effect.
    ///   2. The pickup then becomes inactive (visual hidden, trigger ignored) and
    ///      schedules a coroutine to re-activate after <see cref="_respawnDelay"/>
    ///      seconds (time-scale–affected; pauses when the game is paused).
    ///   3. <see cref="TriggerPickup"/> exposes the same pickup logic as a
    ///      direct method call so EditMode tests can exercise it without
    ///      needing a physics simulation or trigger infrastructure.
    ///
    /// ── Scene wiring ─────────────────────────────────────────────────────────────
    ///   1. Add this MB to a GameObject that has a Collider set to <b>Is Trigger</b>.
    ///   2. Assign <c>_powerUp</c> → a <see cref="PowerUpSO"/> asset.
    ///   3. Optionally assign <c>_visualRoot</c> → the child GameObject to
    ///      show / hide when the pickup is collected / respawns.
    ///   4. Override <c>_respawnDelay</c> per-pickup or use a shared
    ///      <see cref="PowerUpSpawnConfig"/> to drive it from an arena-level SO.
    ///
    /// ── Architecture ─────────────────────────────────────────────────────────────
    ///   • <c>BattleRobots.Physics</c> namespace — may reference Core; must not
    ///     reference <c>BattleRobots.UI</c>.
    ///   • <see cref="OnTriggerEnter"/> is the Unity entry-point; it immediately
    ///     delegates to <see cref="TriggerPickup"/> to keep logic testable.
    ///   • No allocation in the trigger path beyond the one-off
    ///     <c>GetComponentInParent</c> call.
    ///   • No <c>Update</c> or <c>FixedUpdate</c> — all timing via coroutine.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PowerUpController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Power-Up Config")]
        [Tooltip("The SO that defines this pickup's effect type and amount.")]
        [SerializeField] private PowerUpSO _powerUp;

        [Tooltip("Seconds before this pickup respawns after being collected. " +
                 "Pauses when Time.timeScale is 0 (i.e. game is paused).")]
        [SerializeField, Min(0f)] private float _respawnDelay = 15f;

        [Header("Visual (optional)")]
        [Tooltip("Child GameObject to deactivate on pickup and reactivate on respawn. " +
                 "Leave null when the pickup's visual is managed elsewhere.")]
        [SerializeField] private GameObject _visualRoot;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool      _isActive        = true;
        private Coroutine _respawnCoroutine;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>
        /// True while this pickup can be collected.
        /// False from the moment it is collected until the respawn coroutine completes.
        /// </summary>
        public bool IsActive => _isActive;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            _isActive = true;
            if (_visualRoot != null)
                _visualRoot.SetActive(true);
        }

        private void OnDisable()
        {
            if (_respawnCoroutine != null)
            {
                StopCoroutine(_respawnCoroutine);
                _respawnCoroutine = null;
            }
        }

        // ── Unity physics callback ─────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            // Delegate to testable API — keeps the Unity callback a thin wrapper.
            DamageReceiver dr = other.GetComponentInParent<DamageReceiver>();
            TriggerPickup(dr);
        }

        // ── Public testable API ────────────────────────────────────────────────

        /// <summary>
        /// Attempts to apply this pickup's effect to <paramref name="dr"/>.
        ///
        /// Guards (all no-op):
        ///   • <see cref="IsActive"/> is false (already collected, awaiting respawn).
        ///   • <see cref="_powerUp"/> is null (no config assigned in Inspector).
        ///   • <paramref name="dr"/> is null (no <see cref="DamageReceiver"/> on the collider).
        ///   • <paramref name="dr"/>.IsDead is true (dead robots cannot benefit).
        ///
        /// On success: applies the effect, fires the optional pickup event,
        /// deactivates the visual, and starts the respawn coroutine.
        ///
        /// Exposed as a public method so EditMode tests can call it directly
        /// without needing a physics simulation.
        /// </summary>
        public void TriggerPickup(DamageReceiver dr)
        {
            if (!_isActive || _powerUp == null) return;
            if (dr == null || dr.IsDead)        return;

            ApplyEffect(dr);
            _powerUp.FirePickedUp();

            _isActive = false;
            if (_visualRoot != null)
                _visualRoot.SetActive(false);

            // Only start coroutine when running in play-mode (not EditMode tests).
            if (Application.isPlaying && _respawnDelay > 0f)
                _respawnCoroutine = StartCoroutine(RespawnCoroutine());
            else if (Application.isPlaying)
            {
                // Zero delay — respawn immediately.
                _isActive = true;
                if (_visualRoot != null)
                    _visualRoot.SetActive(true);
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private void ApplyEffect(DamageReceiver dr)
        {
            switch (_powerUp.Type)
            {
                case PowerUpType.HealthRestore:
                    dr.Heal(_powerUp.EffectAmount);
                    break;

                case PowerUpType.ShieldRecharge:
                    dr.RestoreShield(_powerUp.EffectAmount);
                    break;
            }
        }

        private IEnumerator RespawnCoroutine()
        {
            yield return new WaitForSeconds(_respawnDelay);

            _isActive = true;
            if (_visualRoot != null)
                _visualRoot.SetActive(true);

            _respawnCoroutine = null;
        }
    }
}

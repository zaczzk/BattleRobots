using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Grants brief invulnerability to a robot immediately after it respawns by
    /// temporarily overriding its <see cref="DamageReceiver"/> armor rating.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   1. Subscribes to <c>_onRespawnReady</c> in OnEnable.
    ///   2. When the event fires, <see cref="HandleRespawnReady"/> saves the current
    ///      armor rating, applies <see cref="RespawnProtectionSO.FullArmorRating"/>,
    ///      and starts the protection timer.
    ///   3. Each <see cref="Tick"/> call advances the timer; once
    ///      <see cref="RespawnProtectionSO.ProtectionDuration"/> elapses,
    ///      <see cref="EndProtection"/> restores the saved armor rating.
    ///   4. OnDisable restores the original armor if protection is still active,
    ///      preventing a stale over-armored robot after the component is toggled off.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Physics namespace — references DamageReceiver.
    ///   - BattleRobots.UI must NOT reference this class.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; Update calls Tick(Time.deltaTime) — zero alloc.
    ///   - DisallowMultipleComponent — one protection controller per robot.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_protectionSO</c> → a RespawnProtectionSO asset.
    ///   2. Assign <c>_receiver</c>     → the robot's DamageReceiver component.
    ///   3. Assign <c>_onRespawnReady</c> → the same VoidGameEvent wired to
    ///      RobotRespawnSO (fired when the cooldown expires and the robot re-enters).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RespawnProtectionController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("SO holding protection duration and the armor rating to apply.")]
        [SerializeField] private RespawnProtectionSO _protectionSO;

        [Header("Target (optional)")]
        [Tooltip("DamageReceiver whose armor is temporarily overridden during the protection window.")]
        [SerializeField] private DamageReceiver _receiver;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent fired by RobotRespawnSO when the respawn cooldown expires.")]
        [SerializeField] private VoidGameEvent _onRespawnReady;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _isProtected;
        private float _elapsed;
        private int   _originalArmor;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _handleRespawnReadyDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleRespawnReadyDelegate = HandleRespawnReady;
        }

        private void OnEnable()
        {
            _onRespawnReady?.RegisterCallback(_handleRespawnReadyDelegate);
        }

        private void OnDisable()
        {
            _onRespawnReady?.UnregisterCallback(_handleRespawnReadyDelegate);

            // Restore armor immediately so a disabled component does not leave
            // the robot permanently over-armored.
            if (_isProtected)
                RestoreArmor();
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances the protection timer by <paramref name="deltaTime"/> seconds.
        /// Calls <see cref="EndProtection"/> once <see cref="RespawnProtectionSO.ProtectionDuration"/>
        /// has elapsed.
        /// No-op when not currently protected.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_isProtected || _protectionSO == null) return;

            _elapsed += deltaTime;

            if (_elapsed >= _protectionSO.ProtectionDuration)
                EndProtection();
        }

        /// <summary>
        /// Begins the invulnerability window: saves the current armor rating, applies
        /// <see cref="RespawnProtectionSO.FullArmorRating"/>, and starts the timer.
        /// No-op when <see cref="_receiver"/> or <see cref="_protectionSO"/> is null.
        /// Wired to <c>_onRespawnReady</c>.
        /// </summary>
        public void HandleRespawnReady()
        {
            if (_receiver == null || _protectionSO == null) return;

            _originalArmor = _receiver.ArmorRating;
            _receiver.SetArmorRating(_protectionSO.FullArmorRating);
            _isProtected = true;
            _elapsed     = 0f;
        }

        /// <summary>
        /// Ends the invulnerability window and restores the saved armor rating.
        /// No-op when not currently protected.
        /// </summary>
        public void EndProtection()
        {
            if (!_isProtected) return;
            RestoreArmor();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while the invulnerability window is active.</summary>
        public bool IsProtected => _isProtected;

        /// <summary>Seconds elapsed since protection began (0 when not protected).</summary>
        public float Elapsed => _elapsed;

        /// <summary>The assigned <see cref="RespawnProtectionSO"/>. May be null.</summary>
        public RespawnProtectionSO ProtectionSO => _protectionSO;

        /// <summary>The assigned <see cref="DamageReceiver"/>. May be null.</summary>
        public DamageReceiver Receiver => _receiver;

        // ── Private helpers ───────────────────────────────────────────────────

        private void RestoreArmor()
        {
            _isProtected = false;
            if (_receiver != null)
                _receiver.SetArmorRating(_originalArmor);
        }
    }
}

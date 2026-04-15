using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// In-match HUD controller that displays a countdown shield bar while the local
    /// respawn-protection timer is active.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   Awake      → caches _handleRespawnReadyDelegate.
    ///   OnEnable   → subscribes _onRespawnReady → HandleRespawnReady; Refresh().
    ///   OnDisable  → unsubscribes.
    ///   Update     → calls Tick(Time.deltaTime).
    ///
    ///   HandleRespawnReady():
    ///     • Null-guard _protectionSO → no-op.
    ///     • Sets _isProtected=true, _elapsed=0, shows _panel.
    ///
    ///   Tick(dt):
    ///     • No-op when !_isProtected or _protectionSO null.
    ///     • Accumulates _elapsed.
    ///     • Updates _shieldBar.value = remaining / ProtectionDuration  [1→0].
    ///     • Updates _timerLabel.text = "{remaining:F1}s".
    ///     • Calls EndProtection() once _elapsed ≥ ProtectionDuration.
    ///
    ///   EndProtection():
    ///     • _isProtected=false; hides _panel.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • Timer is driven by the same _onRespawnReady event the Physics layer
    ///     uses, keeping BattleRobots.UI fully decoupled from Physics.
    ///   • ProtectionDuration is read from the Core-layer RespawnProtectionSO.
    ///   • No Update/FixedUpdate per-frame allocations — Tick uses float arithmetic.
    ///   • DisallowMultipleComponent — one shield panel per HUD canvas.
    ///
    /// Scene wiring:
    ///   _protectionSO   → RespawnProtectionSO (provides ProtectionDuration).
    ///   _onRespawnReady → same VoidGameEvent wired to RobotRespawnSO.
    ///   _shieldBar      → Slider driven from 1 (full) to 0 (expired).
    ///   _timerLabel     → Text showing remaining seconds "N.Fs".
    ///   _panel          → Root panel (hidden when not protected).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RespawnProtectionHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Provides ProtectionDuration for the countdown timer.")]
        [SerializeField] private RespawnProtectionSO _protectionSO;

        [Header("Event Channel — In (optional)")]
        [Tooltip("VoidGameEvent fired when the respawn cooldown expires (protection begins).")]
        [SerializeField] private VoidGameEvent _onRespawnReady;

        [Header("UI References (optional)")]
        [Tooltip("Slider driven from 1→0 over the protection window.")]
        [SerializeField] private Slider _shieldBar;

        [Tooltip("Text label showing remaining protection time as 'N.Fs'.")]
        [SerializeField] private Text _timerLabel;

        [Tooltip("Root panel shown during protection, hidden otherwise.")]
        [SerializeField] private GameObject _panel;

        // ── Runtime state ─────────────────────────────────────────────────────

        private bool  _isProtected;
        private float _elapsed;

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
            Refresh();
        }

        private void OnDisable()
        {
            _onRespawnReady?.UnregisterCallback(_handleRespawnReadyDelegate);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Begins the local protection countdown: starts the timer and shows the panel.
        /// No-op when <see cref="_protectionSO"/> is null.
        /// Wired to <c>_onRespawnReady</c>.
        /// </summary>
        public void HandleRespawnReady()
        {
            if (_protectionSO == null) return;

            _isProtected = true;
            _elapsed     = 0f;
            _panel?.SetActive(true);
        }

        /// <summary>
        /// Advances the protection countdown by <paramref name="dt"/> seconds.
        /// Updates <c>_shieldBar</c> and <c>_timerLabel</c> each frame.
        /// Calls <see cref="EndProtection"/> once <see cref="RespawnProtectionSO.ProtectionDuration"/>
        /// elapses.
        /// No-op when not currently protected or <c>_protectionSO</c> is null.
        /// Zero allocation — float arithmetic only.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isProtected || _protectionSO == null) return;

            _elapsed += dt;

            float duration  = _protectionSO.ProtectionDuration;
            float remaining = Mathf.Max(0f, duration - _elapsed);
            float ratio     = (duration > 0f) ? (remaining / duration) : 0f;

            if (_shieldBar != null)
                _shieldBar.value = ratio;

            if (_timerLabel != null)
                _timerLabel.text = $"{remaining:F1}s";

            if (_elapsed >= duration)
                EndProtection();
        }

        /// <summary>
        /// Ends the invulnerability window and hides the HUD panel.
        /// No-op when not currently protected.
        /// </summary>
        public void EndProtection()
        {
            if (!_isProtected) return;

            _isProtected = false;
            _panel?.SetActive(false);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void Refresh()
        {
            if (_panel == null) return;
            _panel.SetActive(_isProtected);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>True while the local protection countdown is active.</summary>
        public bool IsProtected => _isProtected;

        /// <summary>Seconds elapsed since protection began (0 when not protected).</summary>
        public float Elapsed => _elapsed;

        /// <summary>The assigned <see cref="RespawnProtectionSO"/>. May be null.</summary>
        public RespawnProtectionSO ProtectionSO => _protectionSO;
    }
}

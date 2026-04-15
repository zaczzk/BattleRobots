using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// MonoBehaviour that applies per-zone cooldown durations from a
    /// <see cref="ZoneTimerExtensionConfig"/> to a parallel array of
    /// <see cref="ZoneTimerSO"/> assets via <c>SetCooldownDuration</c>.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   OnEnable → Apply():
    ///     • For each index i in _timers[]:
    ///         _timers[i].SetCooldownDuration(_config.GetCooldownDuration(i))
    ///     • Null-safe — no-op when _config or _timers is null.
    ///     • Null entries in _timers[] are skipped.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Command-driven only — no Update/FixedUpdate.
    ///   - All inspector refs optional; null-safe throughout.
    ///   - DisallowMultipleComponent — one controller per game-object.
    ///
    /// Scene wiring:
    ///   _config → ZoneTimerExtensionConfig asset.
    ///   _timers → Array of ZoneTimerSO assets (one per capturable zone),
    ///             indexed to match _config._cooldownDurations[].
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneTimerExtensionController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Config (optional)")]
        [Tooltip("Per-zone cooldown duration table.")]
        [SerializeField] private ZoneTimerExtensionConfig _config;

        [Header("Timers (optional)")]
        [Tooltip("Zone timer SOs to configure, indexed to match _config._cooldownDurations[].")]
        [SerializeField] private ZoneTimerSO[] _timers;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Apply();

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies per-zone cooldown durations from <see cref="_config"/> to each
        /// non-null <see cref="ZoneTimerSO"/> in <see cref="_timers"/>.
        /// No-op when _config or _timers is null. Null entries in _timers are skipped.
        /// Zero allocation — array iteration only.
        /// </summary>
        public void Apply()
        {
            if (_config == null || _timers == null) return;

            for (int i = 0; i < _timers.Length; i++)
            {
                if (_timers[i] == null) continue;
                _timers[i].SetCooldownDuration(_config.GetCooldownDuration(i));
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneTimerExtensionConfig"/>. May be null.</summary>
        public ZoneTimerExtensionConfig Config => _config;

        /// <summary>Number of <see cref="ZoneTimerSO"/> entries in the timers array.</summary>
        public int TimerCount => _timers?.Length ?? 0;
    }
}

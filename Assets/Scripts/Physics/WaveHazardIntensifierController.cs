using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Scales the toggle interval of all managed <see cref="HazardZoneGroupToggleController"/>
    /// instances each time a new wave begins, making hazards cycle faster as the wave number
    /// increases.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   1. Subscribes to <c>_onWaveStarted</c> in OnEnable.
    ///   2. On <see cref="HandleWaveStarted"/>:
    ///        a. Reads <see cref="WaveManagerSO.CurrentWave"/> from <c>_waveManager</c>.
    ///        b. Calls <see cref="WaveHazardIntensifierSO.GetIntervalForWave"/> to
    ///           compute the new interval.
    ///        c. Calls <see cref="HazardZoneGroupToggleController.SetToggleInterval"/>
    ///           on every entry in <c>_toggleControllers</c>.
    ///   3. All refs are optional; null entries in <c>_toggleControllers</c> are skipped.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Physics namespace — references Core SOs and
    ///     HazardZoneGroupToggleController (Physics).
    ///   - BattleRobots.UI must NOT reference this class.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one intensifier per arena.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_intensifierSO</c>      → a WaveHazardIntensifierSO asset.
    ///   2. Assign <c>_waveManager</c>        → the WaveManagerSO asset.
    ///   3. Assign <c>_onWaveStarted</c>      → the same VoidGameEvent wired to
    ///                                          WaveManagerSO._onWaveStarted.
    ///   4. Assign <c>_toggleControllers</c>  → all HazardZoneGroupToggleControllers
    ///                                          whose interval should scale with waves.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaveHazardIntensifierController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Provides GetIntervalForWave(wave) formula.")]
        [SerializeField] private WaveHazardIntensifierSO _intensifierSO;

        [Tooltip("Provides CurrentWave on each wave-started event.")]
        [SerializeField] private WaveManagerSO _waveManager;

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised by WaveManagerSO at the start of each wave.")]
        [SerializeField] private VoidGameEvent _onWaveStarted;

        [Header("Targets (optional)")]
        [Tooltip("HazardZoneGroupToggleControllers whose toggle interval will be " +
                 "updated to the wave-scaled value on each new wave.")]
        [SerializeField] private HazardZoneGroupToggleController[] _toggleControllers;

        // ── Cached delegate ───────────────────────────────────────────────────

        private Action _waveStartDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _waveStartDelegate = HandleWaveStarted;
        }

        private void OnEnable()
        {
            _onWaveStarted?.RegisterCallback(_waveStartDelegate);
        }

        private void OnDisable()
        {
            _onWaveStarted?.UnregisterCallback(_waveStartDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current wave from <c>_waveManager</c>, computes the new interval
        /// via <c>_intensifierSO</c>, and applies it to all <c>_toggleControllers</c>.
        /// No-op when <c>_waveManager</c> or <c>_intensifierSO</c> is null.
        /// Null entries in <c>_toggleControllers</c> are skipped.
        /// Wired to <c>_onWaveStarted</c>.
        /// </summary>
        public void HandleWaveStarted()
        {
            if (_waveManager == null || _intensifierSO == null) return;

            int   wave     = _waveManager.CurrentWave;
            float interval = _intensifierSO.GetIntervalForWave(wave);

            if (_toggleControllers == null) return;

            foreach (HazardZoneGroupToggleController ctrl in _toggleControllers)
            {
                ctrl?.SetToggleInterval(interval);
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="WaveHazardIntensifierSO"/>. May be null.</summary>
        public WaveHazardIntensifierSO IntensifierSO => _intensifierSO;

        /// <summary>The assigned <see cref="WaveManagerSO"/>. May be null.</summary>
        public WaveManagerSO WaveManager => _waveManager;
    }
}

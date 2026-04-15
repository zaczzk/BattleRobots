using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// MonoBehaviour that applies a difficulty-scaled capture time factor to all
    /// monitored <see cref="ControlZoneController"/> instances at match start.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   _onMatchStarted fires → Apply():
    ///     • Reads <see cref="_scalerSO"/>.GetCaptureTimeScale(_difficultyIndex).
    ///     • Calls <see cref="ControlZoneController.SetCaptureTimeScale"/> on
    ///       each non-null entry in <see cref="_zoneControllers"/>.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Physics namespace.
    ///   - DisallowMultipleComponent — one scaler per arena.
    ///   - All refs optional; null refs produce a silent no-op.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///   - <c>_difficultyIndex</c> maps to the designer-configured preset index
    ///     (e.g. 0 = Easy, 1 = Normal, 2 = Hard).
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   _scalerSO        → ZoneControlDifficultyScalerSO asset.
    ///   _zoneControllers → All ControlZoneController instances in the arena.
    ///   _difficultyIndex → Difficulty index to look up in _scalerSO (0-based).
    ///   _onMatchStarted  → VoidGameEvent raised by MatchManager at match start.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlDifficultyScaler : MonoBehaviour
    {
        // ── Inspector — Data ──────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("SO providing the capture time scale per difficulty index.")]
        [SerializeField] private ZoneControlDifficultyScalerSO _scalerSO;

        [Tooltip("Target zone controllers to update at match start.")]
        [SerializeField] private ControlZoneController[] _zoneControllers;

        [Header("Difficulty Settings")]
        [Tooltip("Zero-based difficulty preset index passed to ZoneControlDifficultyScalerSO. " +
                 "0 = first preset (e.g. Easy), 1 = second preset (e.g. Normal), etc.")]
        [SerializeField, Min(0)] private int _difficultyIndex;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _applyDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _applyDelegate = Apply;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_applyDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_applyDelegate);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the capture time scale for <see cref="_difficultyIndex"/> and
        /// applies it to all non-null zone controllers.
        /// Null-safe on all data refs.
        /// </summary>
        public void Apply()
        {
            if (_scalerSO == null || _zoneControllers == null) return;

            float scale = _scalerSO.GetCaptureTimeScale(_difficultyIndex);

            foreach (ControlZoneController ctrl in _zoneControllers)
                ctrl?.SetCaptureTimeScale(scale);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ZoneControlDifficultyScalerSO"/>. May be null.</summary>
        public ZoneControlDifficultyScalerSO ScalerSO => _scalerSO;

        /// <summary>The current difficulty index used for scale lookup.</summary>
        public int DifficultyIndex => _difficultyIndex;
    }
}

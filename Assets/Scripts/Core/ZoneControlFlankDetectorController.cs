using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that feeds bot zone-capture events into
    /// <see cref="ZoneControlFlankDetectorSO"/> and resets it at match start.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no UI / Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one flank detector per arena manager.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _detectorSO         → ZoneControlFlankDetectorSO asset.
    ///   2. Assign _onBotZoneCaptured  → IntGameEvent raised per bot capture (value = zone index).
    ///   3. Assign _onMatchStarted     → VoidGameEvent raised at match start.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlFlankDetectorController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlFlankDetectorSO _detectorSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("IntGameEvent raised per bot zone capture; value is the zone index.")]
        [SerializeField] private IntGameEvent  _onBotZoneCaptured;

        [Tooltip("Raised at match start; resets the detector SO state.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action<int> _handleBotCapturedDelegate;
        private Action      _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleBotCapturedDelegate  = HandleBotZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onBotZoneCaptured?.RegisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
        }

        private void OnDisable()
        {
            _onBotZoneCaptured?.UnregisterCallback(_handleBotCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Forwards a bot zone-capture event to the detector SO at the current timestamp.
        /// </summary>
        public void HandleBotZoneCaptured(int zoneIndex)
        {
            _detectorSO?.RecordBotCapture(zoneIndex, Time.time);
        }

        /// <summary>Resets the detector SO at match start.</summary>
        public void HandleMatchStarted()
        {
            _detectorSO?.Reset();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound flank detector SO (may be null).</summary>
        public ZoneControlFlankDetectorSO DetectorSO => _detectorSO;
    }
}

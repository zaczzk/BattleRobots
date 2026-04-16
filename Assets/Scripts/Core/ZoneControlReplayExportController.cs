using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that triggers a <see cref="ZoneControlReplayExportSO"/>
    /// export automatically at the end of each match.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   When <c>_onMatchEnded</c> fires and <c>_autoExportOnMatchEnd</c> is true,
    ///   <see cref="TriggerExport"/> is called, which delegates to
    ///   <see cref="ZoneControlReplayExportSO.ExportToJson"/>.
    ///   <see cref="TriggerExport"/> is also public so the designer can call it
    ///   manually from a UI button or other script.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no UI references.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegate cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one export controller per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _exportSO   → ZoneControlReplayExportSO asset.
    ///   2. Assign _replaySO   → ZoneControlReplaySO asset (ring-buffer source).
    ///   3. Assign _onMatchEnded → shared MatchEnded VoidGameEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlReplayExportController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlReplayExportSO _exportSO;
        [SerializeField] private ZoneControlReplaySO       _replaySO;

        [Header("Settings")]
        [Tooltip("When true, ExportToJson is called automatically whenever the match ends.")]
        [SerializeField] private bool _autoExportOnMatchEnd = true;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to the shared MatchEnded VoidGameEvent.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Called when a match ends.  Triggers an export when
        /// <c>_autoExportOnMatchEnd</c> is true.
        /// Null-safe.
        /// </summary>
        public void HandleMatchEnded()
        {
            if (_autoExportOnMatchEnd)
                TriggerExport();
        }

        /// <summary>
        /// Exports the current replay buffer to JSON via the bound
        /// <see cref="ZoneControlReplayExportSO"/>.
        /// No-op when either SO is null.
        /// </summary>
        public void TriggerExport()
        {
            _exportSO?.ExportToJson(_replaySO);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound export SO (may be null).</summary>
        public ZoneControlReplayExportSO ExportSO => _exportSO;

        /// <summary>The bound replay ring-buffer SO (may be null).</summary>
        public ZoneControlReplaySO ReplaySO => _replaySO;

        /// <summary>Whether an export is triggered automatically at match end.</summary>
        public bool AutoExportOnMatchEnd => _autoExportOnMatchEnd;
    }
}

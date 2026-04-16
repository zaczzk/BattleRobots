using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that triggers a session export when the session ends.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   When <c>_autoExportOnSessionEnd</c> is true (default), subscribes
    ///   <c>_onSessionEnded</c> → <see cref="HandleSessionEnded"/> →
    ///   <see cref="TriggerExport"/>.
    ///   <see cref="TriggerExport"/> delegates to
    ///   <c>_exportSO.ExportSession(_summarySO, _roundResultSO, _ratingHistorySO)</c>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no UI references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one exporter per session.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlSessionExportController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlSessionExportSO          _exportSO;
        [SerializeField] private ZoneControlSessionSummarySO         _summarySO;
        [SerializeField] private ZoneControlRoundResultSO            _roundResultSO;
        [SerializeField] private ZoneControlMatchRatingHistorySO     _ratingHistorySO;

        [Header("Settings")]
        [Tooltip("When true, export is triggered automatically when _onSessionEnded fires.")]
        [SerializeField] private bool _autoExportOnSessionEnd = true;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised at session end; triggers TriggerExport when auto-export is on.")]
        [SerializeField] private VoidGameEvent _onSessionEnded;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleSessionEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleSessionEndedDelegate = HandleSessionEnded;
        }

        private void OnEnable()
        {
            _onSessionEnded?.RegisterCallback(_handleSessionEndedDelegate);
        }

        private void OnDisable()
        {
            _onSessionEnded?.UnregisterCallback(_handleSessionEndedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Called when the session ends. Triggers export when
        /// <c>_autoExportOnSessionEnd</c> is true.
        /// </summary>
        public void HandleSessionEnded()
        {
            if (_autoExportOnSessionEnd)
                TriggerExport();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Calls <c>ExportSession</c> on the bound export SO with all data SOs.
        /// No-op when <c>_exportSO</c> is null.
        /// </summary>
        public void TriggerExport()
        {
            _exportSO?.ExportSession(_summarySO, _roundResultSO, _ratingHistorySO);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound export SO (may be null).</summary>
        public ZoneControlSessionExportSO ExportSO => _exportSO;

        /// <summary>Whether auto-export on session end is enabled.</summary>
        public bool AutoExportOnSessionEnd => _autoExportOnSessionEnd;
    }
}

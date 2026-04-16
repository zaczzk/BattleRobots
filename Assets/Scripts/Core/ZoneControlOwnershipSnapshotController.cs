using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that captures zone ownership at match end via
    /// <see cref="ZoneControlOwnershipSnapshotSO.TakeSnapshot"/>.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Subscribes <c>_onMatchEnded</c> → <see cref="HandleMatchEnded"/> which
    ///   calls <see cref="ZoneControlOwnershipSnapshotSO.TakeSnapshot"/> with the
    ///   current catalog ownership state.
    ///   The snapshot SO fires <c>_onSnapshotTaken</c> after each capture.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegate cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one snapshot controller per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _snapshotSO → ZoneControlOwnershipSnapshotSO asset.
    ///   2. Assign _catalogSO  → ZoneControlZoneControllerCatalogSO asset.
    ///   3. Assign _onMatchEnded → shared MatchEnded VoidGameEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlOwnershipSnapshotController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlOwnershipSnapshotSO    _snapshotSO;
        [SerializeField] private ZoneControlZoneControllerCatalogSO _catalogSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Raised at match end; triggers a snapshot.")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake() => _handleMatchEndedDelegate = HandleMatchEnded;

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Captures the current catalog ownership state into <c>_snapshotSO</c>.
        /// No-op when either SO is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            _snapshotSO?.TakeSnapshot(_catalogSO);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound ownership snapshot SO (may be null).</summary>
        public ZoneControlOwnershipSnapshotSO SnapshotSO => _snapshotSO;
    }
}

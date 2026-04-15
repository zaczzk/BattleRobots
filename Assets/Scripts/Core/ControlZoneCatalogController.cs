using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that manages the lifecycle of all zones in a
    /// <see cref="ControlZoneCatalogSO"/> by resetting them at match start and end.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   1. On <c>_onMatchStarted</c>: calls <see cref="ControlZoneCatalogSO.ResetAll"/>
    ///      so every zone begins the match in a clean, uncaptured state.
    ///   2. On <c>_onMatchEnded</c>: calls <see cref="ControlZoneCatalogSO.ResetAll"/>
    ///      to clear zone state after the match completes.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace — no Physics / UI dependencies.
    ///   - All refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_catalog</c>          → a ControlZoneCatalogSO asset.
    ///   2. Assign <c>_onMatchStarted</c>   → shared match-start VoidGameEvent.
    ///   3. Assign <c>_onMatchEnded</c>     → shared match-end VoidGameEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ControlZoneCatalogController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("The ControlZoneCatalogSO whose zones are reset at match boundaries.")]
        [SerializeField] private ControlZoneCatalogSO _catalog;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _startDelegate;
        private Action _endDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _startDelegate = HandleMatchStarted;
            _endDelegate   = HandleMatchEnded;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_startDelegate);
            _onMatchEnded?.RegisterCallback(_endDelegate);
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_startDelegate);
            _onMatchEnded?.UnregisterCallback(_endDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Resets all zones in the catalog to an uncaptured state.
        /// Wired to <c>_onMatchStarted</c>.
        /// </summary>
        public void HandleMatchStarted()
        {
            _catalog?.ResetAll();
        }

        /// <summary>
        /// Resets all zones in the catalog after the match ends.
        /// Wired to <c>_onMatchEnded</c>.
        /// </summary>
        public void HandleMatchEnded()
        {
            _catalog?.ResetAll();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ControlZoneCatalogSO"/>. May be null.</summary>
        public ControlZoneCatalogSO Catalog => _catalog;

        /// <summary>
        /// Number of entries in the catalog. Returns 0 when catalog is null.
        /// </summary>
        public int EntryCount => _catalog?.EntryCount ?? 0;
    }
}

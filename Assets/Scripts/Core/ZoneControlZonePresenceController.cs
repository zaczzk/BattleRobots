using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that keeps <see cref="ZoneControlZoneControllerCatalogSO"/>
    /// up-to-date by subscribing to player and bot zone-capture events.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Subscribes <c>_onPlayerZoneCaptured</c> (IntGameEvent) →
    ///       <see cref="HandlePlayerZoneCaptured"/> →
    ///       <c>SetZoneController(index, playerOwned: true)</c>.
    ///   Subscribes <c>_onBotZoneCaptured</c> (IntGameEvent) →
    ///       <see cref="HandleBotZoneCaptured"/> →
    ///       <c>SetZoneController(index, playerOwned: false)</c>.
    ///   Subscribes <c>_onMatchStarted</c> (VoidGameEvent) →
    ///       <see cref="HandleMatchStarted"/> → <c>Reset()</c>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no UI references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one presence tracker per scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlZonePresenceController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneControllerCatalogSO _catalogSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("IntGameEvent carrying zone index; marks that zone as player-owned.")]
        [SerializeField] private IntGameEvent _onPlayerZoneCaptured;

        [Tooltip("IntGameEvent carrying zone index; marks that zone as bot-owned.")]
        [SerializeField] private IntGameEvent _onBotZoneCaptured;

        [Tooltip("Raised at match start; resets all zone ownership.")]
        [SerializeField] private VoidGameEvent _onMatchStarted;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action<int> _handlePlayerZoneCapturedDelegate;
        private Action<int> _handleBotZoneCapturedDelegate;
        private Action      _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handlePlayerZoneCapturedDelegate = HandlePlayerZoneCaptured;
            _handleBotZoneCapturedDelegate    = HandleBotZoneCaptured;
            _handleMatchStartedDelegate       = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerZoneCapturedDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerZoneCapturedDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Marks the zone at <paramref name="zoneIndex"/> as player-owned.
        /// </summary>
        public void HandlePlayerZoneCaptured(int zoneIndex)
        {
            _catalogSO?.SetZoneController(zoneIndex, true);
        }

        /// <summary>
        /// Marks the zone at <paramref name="zoneIndex"/> as bot-owned.
        /// </summary>
        public void HandleBotZoneCaptured(int zoneIndex)
        {
            _catalogSO?.SetZoneController(zoneIndex, false);
        }

        /// <summary>
        /// Resets all zone ownership to bot-controlled at match start.
        /// </summary>
        public void HandleMatchStarted()
        {
            _catalogSO?.Reset();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound catalog SO (may be null).</summary>
        public ZoneControlZoneControllerCatalogSO CatalogSO => _catalogSO;
    }
}

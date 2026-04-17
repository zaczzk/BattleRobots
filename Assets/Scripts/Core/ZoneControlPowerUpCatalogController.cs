using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that routes spawn-request events to
    /// <see cref="ZoneControlPowerUpCatalogSO.SelectRandom"/> and resets the
    /// catalog at match boundaries.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On <c>_onSpawnPowerUp</c>: calls <see cref="ZoneControlPowerUpCatalogSO.SelectRandom"/>.
    ///   On <c>_onMatchStarted</c>: calls <see cref="ZoneControlPowerUpCatalogSO.Reset"/>
    ///   so the last-selected index is cleared at the start of each match.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no UI references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one catalog controller per scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlPowerUpCatalogController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlPowerUpCatalogSO _catalogSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onSpawnPowerUp;
        [SerializeField] private VoidGameEvent _onMatchStarted;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleSpawnPowerUpDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleSpawnPowerUpDelegate = HandleSpawnPowerUp;
            _handleMatchStartedDelegate = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onSpawnPowerUp?.RegisterCallback(_handleSpawnPowerUpDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
        }

        private void OnDisable()
        {
            _onSpawnPowerUp?.UnregisterCallback(_handleSpawnPowerUpDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>Selects a random power-up from the catalog. No-op when catalog is null.</summary>
        public void HandleSpawnPowerUp() => _catalogSO?.SelectRandom();

        /// <summary>Resets the catalog selection state. No-op when catalog is null.</summary>
        public void HandleMatchStarted() => _catalogSO?.Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound catalog SO (may be null).</summary>
        public ZoneControlPowerUpCatalogSO CatalogSO => _catalogSO;
    }
}

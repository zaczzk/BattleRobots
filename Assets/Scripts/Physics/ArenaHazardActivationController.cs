using System;
using BattleRobots.Core;
using UnityEngine;

namespace BattleRobots.Physics
{
    /// <summary>
    /// Activates arena hazard zones at match start according to per-entry delays defined
    /// in <see cref="ArenaHazardCatalogSO"/>. Fires an "all hazards active" event once
    /// every managed hazard has been switched on.
    ///
    /// ── Behaviour ─────────────────────────────────────────────────────────────
    ///   1. On <c>_onMatchStarted</c>: allocates per-hazard timer and activated arrays;
    ///      immediately activates any hazard whose catalog delay is 0.
    ///   2. Each <see cref="Tick"/>: increments per-hazard timers; activates each hazard
    ///      when its timer meets or exceeds the catalog delay.
    ///   3. When no unactivated hazards remain: sets <see cref="AllActivated"/> to true,
    ///      raises <c>_onAllHazardsActive</c>, and calls
    ///      <see cref="ArenaHazardCatalogSO.RaiseAllActive"/>.
    ///   4. On <c>_onMatchEnded</c>: deactivates all managed hazards and clears
    ///      the match-running flag.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Physics namespace — references HazardZoneController.
    ///   - BattleRobots.UI must NOT reference this class.
    ///   - Timer arrays are allocated once at match start (unavoidable); Tick is
    ///     zero-allocation (float arithmetic and bool array access only).
    ///   - <see cref="Tick"/> is public for EditMode test driving.
    ///   - DisallowMultipleComponent — one catalog controller per match context.
    ///
    /// Scene wiring:
    ///   _catalog            → ArenaHazardCatalogSO (delays + optional all-active event).
    ///   _hazards            → HazardZoneController[] (index-aligned with catalog entries).
    ///   _onMatchStarted     → shared match-start VoidGameEvent.
    ///   _onMatchEnded       → shared match-end VoidGameEvent.
    ///   _onAllHazardsActive → optional extra "all active" channel on this controller.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArenaHazardActivationController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Catalog SO that provides per-entry activation delays. " +
                 "When null all hazards activate immediately at match start.")]
        [SerializeField] private ArenaHazardCatalogSO _catalog;

        [Header("Hazards (optional)")]
        [Tooltip("HazardZoneControllers to manage. Index must align with catalog entries " +
                 "when a catalog is assigned.")]
        [SerializeField] private HazardZoneController[] _hazards;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("Event Channel — Out (optional)")]
        [Tooltip("Raised when the last managed hazard becomes active. " +
                 "The catalog SO also carries its own _onAllHazardsActive channel.")]
        [SerializeField] private VoidGameEvent _onAllHazardsActive;

        // ── Runtime state ─────────────────────────────────────────────────────

        private float[] _timers;
        private bool[]  _activated;
        private bool    _matchRunning;
        private bool    _allActivated;

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

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances per-hazard timers by <paramref name="dt"/> seconds and activates any
        /// hazard whose accumulated timer meets or exceeds its catalog delay.
        /// Fires <c>_onAllHazardsActive</c> (and
        /// <see cref="ArenaHazardCatalogSO.RaiseAllActive"/>) once all hazards are active.
        ///
        /// Zero allocation — float arithmetic and bool array access only.
        /// No-op when the match is not running, all hazards are already active, or
        /// <c>_hazards</c> is null or empty.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_matchRunning || _allActivated) return;
            if (_hazards == null || _hazards.Length == 0 || _timers == null) return;

            int remaining = 0;

            for (int i = 0; i < _hazards.Length; i++)
            {
                if (_activated[i]) continue;

                _timers[i] += dt;

                float delay = (_catalog != null && i < _catalog.EntryCount)
                    ? _catalog.GetActivationDelay(i)
                    : 0f;

                if (_timers[i] >= delay)
                {
                    _activated[i] = true;
                    if (_hazards[i] != null)
                        _hazards[i].IsActive = true;
                }
                else
                {
                    remaining++;
                }
            }

            if (remaining == 0)
            {
                _allActivated = true;
                _onAllHazardsActive?.Raise();
                _catalog?.RaiseAllActive();
            }
        }

        /// <summary>
        /// Initialises timer and activation tracking arrays, then activates any hazard
        /// whose catalog delay is 0. Wired to <c>_onMatchStarted</c>.
        /// </summary>
        public void HandleMatchStarted()
        {
            _matchRunning = true;
            _allActivated = false;

            int count  = _hazards != null ? _hazards.Length : 0;
            _timers    = new float[count];
            _activated = new bool[count];

            // Immediately activate hazards with zero delay.
            Tick(0f);
        }

        /// <summary>
        /// Deactivates all managed hazards and clears the match-running flag.
        /// Wired to <c>_onMatchEnded</c>.
        /// </summary>
        public void HandleMatchEnded()
        {
            _matchRunning = false;
            if (_hazards == null) return;
            foreach (HazardZoneController hazard in _hazards)
            {
                if (hazard != null)
                    hazard.IsActive = false;
            }
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="ArenaHazardCatalogSO"/>. May be null.</summary>
        public ArenaHazardCatalogSO Catalog => _catalog;

        /// <summary>True while a match is in progress.</summary>
        public bool IsMatchRunning => _matchRunning;

        /// <summary>True once every managed hazard has been activated this match.</summary>
        public bool AllActivated => _allActivated;
    }
}

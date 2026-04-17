using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Core MonoBehaviour that drives the <see cref="ZoneControlPostMatchCelebrationSO"/>
    /// celebration sequence after each match.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Subscribes <c>_onMatchEnded</c> → <see cref="HandleMatchEnded"/> which calls
    ///   <see cref="ZoneControlPostMatchCelebrationSO.StartCelebration"/>.
    ///   <c>Update</c> forwards <c>Time.deltaTime</c> to
    ///   <see cref="ZoneControlPostMatchCelebrationSO.Tick"/> each frame while
    ///   the SO is assigned, enabling the step timer without allocating delegates.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one celebration controller per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _celebrationSO → ZoneControlPostMatchCelebrationSO asset.
    ///   2. Assign _onMatchEnded  → shared MatchEnded VoidGameEvent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlPostMatchCelebrationController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlPostMatchCelebrationSO _celebrationSO;

        [Header("Event Channels — In (optional)")]
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

        private void Update()
        {
            _celebrationSO?.Tick(Time.deltaTime);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Starts the post-match celebration sequence on the bound SO.
        /// No-op when <c>_celebrationSO</c> is null.
        /// </summary>
        public void HandleMatchEnded()
        {
            _celebrationSO?.StartCelebration();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound celebration SO (may be null).</summary>
        public ZoneControlPostMatchCelebrationSO CelebrationSO => _celebrationSO;
    }
}

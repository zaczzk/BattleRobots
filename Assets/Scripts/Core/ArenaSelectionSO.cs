using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime-only SO that stores the player's current arena selection.
    ///
    /// Follows the PlayerWallet pattern: mutation only via <see cref="Select"/>;
    /// broadcasts via a SO event channel so UI can react without polling.
    ///
    /// Reset() should be called at game start (e.g. from GameBootstrapper or the
    /// ArenaSelector screen opening) to clear any stale editor value.
    ///
    /// Create via:  Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ArenaSelectionSO
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ArenaSelectionSO", order = 1)]
    public sealed class ArenaSelectionSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Event raised whenever the selection changes. No payload — listeners " +
                 "read SelectedArena directly from this SO.")]
        [SerializeField] private VoidGameEvent _onArenaSelected;

        // ── Runtime State ─────────────────────────────────────────────────────

        /// <summary>
        /// The currently selected arena, or null if nothing has been selected yet.
        /// Read-only from outside — mutate only via <see cref="Select"/>.
        /// </summary>
        public ArenaConfig SelectedArena { get; private set; }

        /// <summary>True when a valid arena has been chosen.</summary>
        public bool HasSelection => SelectedArena != null;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Stores <paramref name="arena"/> as the current selection and raises
        /// <see cref="_onArenaSelected"/> so listeners (UI confirmation button,
        /// MatchManager readiness check) can react.
        /// </summary>
        /// <param name="arena">Must not be null.</param>
        public void Select(ArenaConfig arena)
        {
            if (arena == null)
            {
                Debug.LogWarning("[ArenaSelectionSO] Select called with null ArenaConfig — ignored.");
                return;
            }

            SelectedArena = arena;
            _onArenaSelected?.Raise();

            Debug.Log($"[ArenaSelectionSO] Selected arena: '{arena.ArenaName}'.");
        }

        /// <summary>
        /// Clears the current selection. Call when the selector screen opens
        /// to prevent carrying over a stale value between sessions.
        /// </summary>
        public void Reset()
        {
            SelectedArena = null;
        }
    }
}

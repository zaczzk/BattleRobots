using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks which room codes the player has starred
    /// as favourites. Changes are persisted immediately to the SaveSystem so they
    /// survive app restarts without requiring an explicit save trigger.
    ///
    /// Mutation only through the designated mutators (<see cref="AddFavourite"/>,
    /// <see cref="RemoveFavourite"/>, <see cref="Clear"/>), which fire the
    /// <see cref="_onFavouritesChanged"/> SO event channel and persist.
    ///
    /// Lifecycle:
    ///   1. <see cref="GameBootstrapper"/> calls <see cref="LoadFromData"/> at startup
    ///      with <see cref="SaveData.favouriteRoomCodes"/>.
    ///   2. <see cref="FavouriteButtonUI"/> calls <see cref="AddFavourite"/> /
    ///      <see cref="RemoveFavourite"/> in response to player interaction.
    ///   3. Each mutation auto-persists via <see cref="SaveSystem"/>.
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.Core namespace — no Physics or UI references.
    ///   • Asset is read-only at runtime; all mutation through API.
    ///   • <see cref="IsFavourite"/> is O(1) via internal HashSet.
    ///
    /// Create via:  Assets ▶ Create ▶ BattleRobots ▶ Network ▶ FavouriteRoomsSO
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Network/FavouriteRoomsSO", order = 2)]
    public sealed class FavouriteRoomsSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channel")]
        [Tooltip("Raised after any AddFavourite, RemoveFavourite, or Clear call. " +
                 "Wire UI listeners to refresh star button states.")]
        [SerializeField] private VoidGameEvent _onFavouritesChanged;

        // ── Runtime state (transient — not serialised to the SO asset) ────────

        // Source-of-truth list for BuildData(); preserves insertion order.
        private readonly List<string> _favourites = new List<string>();

        // O(1) membership test — rebuilt from _favourites in LoadFromData.
        private readonly HashSet<string> _set =
            new HashSet<string>(System.StringComparer.Ordinal);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Read-only view of all favourited room codes.</summary>
        public System.Collections.Generic.IReadOnlyList<string> Favourites => _favourites;

        /// <summary>Number of currently favourited rooms.</summary>
        public int Count => _favourites.Count;

        /// <summary>
        /// Returns <c>true</c> if <paramref name="roomCode"/> is currently starred.
        /// O(1) — no allocation.
        /// </summary>
        public bool IsFavourite(string roomCode) =>
            !string.IsNullOrEmpty(roomCode) && _set.Contains(roomCode);

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Adds <paramref name="roomCode"/> to the favourites list.
        /// No-ops silently if the code is null, empty, or already favourited.
        /// Persists via <see cref="SaveSystem"/> and fires the change event.
        /// </summary>
        public void AddFavourite(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode)) return;
            if (!_set.Add(roomCode))             return; // already present — idempotent

            _favourites.Add(roomCode);
            _onFavouritesChanged?.Raise();
            PersistFavourites();
        }

        /// <summary>
        /// Removes <paramref name="roomCode"/> from the favourites list.
        /// No-ops silently if the code is null, empty, or not favourited.
        /// Persists via <see cref="SaveSystem"/> and fires the change event.
        /// </summary>
        public void RemoveFavourite(string roomCode)
        {
            if (string.IsNullOrEmpty(roomCode)) return;
            if (!_set.Remove(roomCode))          return; // not present — no-op

            _favourites.Remove(roomCode);
            _onFavouritesChanged?.Raise();
            PersistFavourites();
        }

        /// <summary>
        /// Removes all favourited rooms.
        /// Persists via <see cref="SaveSystem"/> and fires the change event.
        /// No-op if already empty.
        /// </summary>
        public void Clear()
        {
            if (_favourites.Count == 0) return;

            _favourites.Clear();
            _set.Clear();
            _onFavouritesChanged?.Raise();
            PersistFavourites();
        }

        // ── Save / Load bridge ────────────────────────────────────────────────

        /// <summary>
        /// Populates runtime state from a deserialized <see cref="List{T}"/> of room codes.
        /// Call from <see cref="GameBootstrapper"/> immediately after <see cref="SaveSystem.Load"/>.
        /// Duplicate codes are silently de-duplicated. Null/empty codes are skipped.
        /// Does NOT raise <see cref="_onFavouritesChanged"/> — listeners may not be ready yet.
        /// </summary>
        public void LoadFromData(List<string> codes)
        {
            _favourites.Clear();
            _set.Clear();

            if (codes == null) return;

            for (int i = 0; i < codes.Count; i++)
            {
                string code = codes[i];
                if (string.IsNullOrEmpty(code)) continue;
                if (_set.Add(code))
                    _favourites.Add(code);
            }
        }

        /// <summary>
        /// Snapshots the current favourites into a new <see cref="List{T}"/> ready
        /// to be stored in <see cref="SaveData.favouriteRoomCodes"/>.
        /// Allocates — only call from save paths, never from the hot path.
        /// </summary>
        public List<string> BuildData()
        {
            return new List<string>(_favourites);
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        private void PersistFavourites()
        {
            // Load existing save data (preserves all other fields), update the
            // favourites list, then re-save atomically.
            SaveData save = SaveSystem.Load();
            save.favouriteRoomCodes = BuildData();
            SaveSystem.Save(save);
        }
    }
}

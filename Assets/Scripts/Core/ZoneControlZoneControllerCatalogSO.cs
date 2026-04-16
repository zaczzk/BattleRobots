using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject that tracks which team (player or bot) currently
    /// controls each zone in a zone-control match.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Call <see cref="SetZoneController"/> whenever a zone changes hands.
    ///   <see cref="GetZoneController"/> returns the current owner of a zone index.
    ///   <see cref="PlayerOwnedCount"/> gives the number of zones currently held by
    ///   the player.
    ///   Call <see cref="Reset"/> at match start to clear all ownership.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - Runtime state is NOT serialised — call Reset at match start.
    ///   - Zero heap allocation on hot-path methods.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlZoneControllerCatalog.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlZoneControllerCatalog", order = 54)]
    public sealed class ZoneControlZoneControllerCatalogSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Zone Settings")]
        [Tooltip("Number of zones tracked in this match.")]
        [Min(1)]
        [SerializeField] private int _zoneCount = 4;

        [Header("Event Channels (optional)")]
        [Tooltip("Raised whenever a zone changes controller.")]
        [SerializeField] private VoidGameEvent _onControlChanged;

        // ── Runtime state (not serialised) ────────────────────────────────────

        private bool[] _playerOwned;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Reset();

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Total number of zones tracked.</summary>
        public int ZoneCount => _zoneCount;

        /// <summary>Number of zones currently controlled by the player.</summary>
        public int PlayerOwnedCount
        {
            get
            {
                if (_playerOwned == null) return 0;
                int count = 0;
                for (int i = 0; i < _playerOwned.Length; i++)
                    if (_playerOwned[i]) count++;
                return count;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when the player owns the zone at <paramref name="zoneIndex"/>.
        /// Returns false for out-of-bounds indices or before Reset is called.
        /// </summary>
        public bool GetZoneController(int zoneIndex)
        {
            if (_playerOwned == null || zoneIndex < 0 || zoneIndex >= _playerOwned.Length)
                return false;
            return _playerOwned[zoneIndex];
        }

        /// <summary>
        /// Updates ownership of <paramref name="zoneIndex"/> to <paramref name="playerOwned"/>.
        /// Out-of-bounds indices are silently ignored.
        /// Fires <see cref="_onControlChanged"/> after each valid update.
        /// </summary>
        public void SetZoneController(int zoneIndex, bool playerOwned)
        {
            if (_playerOwned == null || zoneIndex < 0 || zoneIndex >= _playerOwned.Length)
                return;

            _playerOwned[zoneIndex] = playerOwned;
            _onControlChanged?.Raise();
        }

        /// <summary>
        /// Resets all zone ownership to unowned (bot-controlled) silently.
        /// Called automatically by <c>OnEnable</c>.
        /// Re-allocates the ownership array if its size changed.
        /// </summary>
        public void Reset()
        {
            int size = Mathf.Max(1, _zoneCount);
            if (_playerOwned == null || _playerOwned.Length != size)
                _playerOwned = new bool[size];
            else
            {
                for (int i = 0; i < _playerOwned.Length; i++)
                    _playerOwned[i] = false;
            }
        }

        private void OnValidate()
        {
            _zoneCount = Mathf.Max(1, _zoneCount);
        }
    }
}

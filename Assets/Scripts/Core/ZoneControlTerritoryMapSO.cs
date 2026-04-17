using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime SO that tracks per-zone player ownership as a territory map.
    ///
    /// Call <see cref="SetPlayerOwned(int, bool)"/> to update zone ownership.
    /// Fires <c>_onOwnershipChanged</c> on every update.
    /// <see cref="Reset"/> clears all ownership silently; called from <c>OnEnable</c>.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ ZoneControlTerritoryMap.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlTerritoryMap", order = 89)]
    public sealed class ZoneControlTerritoryMapSO : ScriptableObject
    {
        [Header("Territory Settings")]
        [Min(1)]
        [SerializeField] private int _zoneCount = 4;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onOwnershipChanged;

        private bool[] _playerOwned;

        private void OnEnable() => Reset();

        public int ZoneCount => _zoneCount;

        public int PlayerOwnedCount
        {
            get
            {
                if (_playerOwned == null) return 0;
                int count = 0;
                foreach (bool owned in _playerOwned)
                    if (owned) count++;
                return count;
            }
        }

        /// <summary>
        /// Sets the ownership state for the given zone index.
        /// Out-of-bounds indices are silently ignored.
        /// </summary>
        public void SetPlayerOwned(int zoneIndex, bool owned)
        {
            EnsureArray();
            if (zoneIndex < 0 || zoneIndex >= _playerOwned.Length) return;
            _playerOwned[zoneIndex] = owned;
            _onOwnershipChanged?.Raise();
        }

        /// <summary>Returns whether the player owns the given zone (false if out of bounds).</summary>
        public bool IsPlayerOwned(int zoneIndex)
        {
            EnsureArray();
            if (zoneIndex < 0 || zoneIndex >= _playerOwned.Length) return false;
            return _playerOwned[zoneIndex];
        }

        /// <summary>Clears all ownership silently.</summary>
        public void Reset()
        {
            _playerOwned = new bool[Mathf.Max(1, _zoneCount)];
        }

        private void EnsureArray()
        {
            if (_playerOwned == null || _playerOwned.Length != Mathf.Max(1, _zoneCount))
                _playerOwned = new bool[Mathf.Max(1, _zoneCount)];
        }
    }
}

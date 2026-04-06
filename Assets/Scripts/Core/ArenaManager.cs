using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Scene-level manager for the battle arena.
    ///
    /// Responsibilities:
    ///   • Holds the <see cref="ArenaConfig"/> SO reference for this arena.
    ///   • Maintains a registry of <see cref="SpawnPointMarker"/> components that
    ///     self-register on Enable/Disable — giving MatchManager live Transform data.
    ///   • Exposes <see cref="OnMatchStarted"/> as the entry point called by the
    ///     MatchStarted event channel (wire via a <see cref="VoidGameEventListener"/>
    ///     MB on the same GameObject, pointing its UnityEvent at this method).
    ///   • After processing the match-start signal, raises <c>_arenaReadyChannel</c>
    ///     so MatchManager knows spawn poses are available.
    ///
    /// Wiring (Inspector):
    ///   1. Assign an <see cref="ArenaConfig"/> asset.
    ///   2. Assign the optional <c>ArenaReady</c> VoidGameEvent SO.
    ///   3. Add a <see cref="VoidGameEventListener"/> MB on the same GameObject,
    ///      set its Event to the shared <c>MatchStarted</c> channel, and wire
    ///      its UnityEvent response to <c>ArenaManager.OnMatchStarted()</c>.
    ///   4. Place <see cref="SpawnPointMarker"/> GameObjects in the scene — they
    ///      self-register; no manual list needed.
    ///
    /// Namespace: BattleRobots.Core — no Physics / UI references.
    /// </summary>
    public sealed class ArenaManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────

        /// <summary>Scene-local singleton. Null when no ArenaManager is loaded.</summary>
        public static ArenaManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Configuration")]
        [Tooltip("SO describing the arena dimensions and spawn point data.")]
        [SerializeField] private ArenaConfig _arenaConfig;

        [Header("Event Channels (Out)")]
        [Tooltip("Raised after ArenaManager processes MatchStarted and spawn poses are ready. " +
                 "Wire MatchManager to this channel.")]
        [SerializeField] private VoidGameEvent _arenaReadyChannel;

        // ── Runtime state ─────────────────────────────────────────────────────

        // Keyed by SpawnIndex for O(1) look-up regardless of registration order.
        private readonly Dictionary<int, SpawnPointMarker> _markers =
            new Dictionary<int, SpawnPointMarker>();

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The ArenaConfig asset wired to this manager.</summary>
        public ArenaConfig Config => _arenaConfig;

        /// <summary>
        /// Returns the world-space position and rotation for the given spawn index.
        ///
        /// Priority:
        ///   1. Scene <see cref="SpawnPointMarker"/> with matching index (live Transform).
        ///   2. Baked <see cref="SpawnPointData"/> from <see cref="ArenaConfig"/> (SO fallback).
        ///
        /// Returns false if neither source can satisfy the index.
        /// </summary>
        public bool GetSpawnPose(int index, out Vector3 position, out Quaternion rotation)
        {
            // Prefer live scene marker (Transform reflects latest authoring).
            if (_markers.TryGetValue(index, out SpawnPointMarker marker))
            {
                position = marker.Position;
                rotation = marker.Rotation;
                return true;
            }

            // Fall back to baked SO data.
            if (_arenaConfig != null && index >= 0 && index < _arenaConfig.SpawnPointCount)
            {
                SpawnPointData data = _arenaConfig.GetSpawnPoint(index);
                position = data.position;
                rotation = data.Rotation;
                return true;
            }

            position = Vector3.zero;
            rotation = Quaternion.identity;
            Debug.LogWarning($"[ArenaManager] No spawn pose found for index {index}.", this);
            return false;
        }

        /// <summary>Number of spawn points available from the ArenaConfig SO.</summary>
        public int SpawnPointCount => _arenaConfig != null ? _arenaConfig.SpawnPointCount : 0;

        // ── Marker registry (called by SpawnPointMarker) ──────────────────────

        /// <summary>Called automatically by <see cref="SpawnPointMarker.OnEnable"/>.</summary>
        public void RegisterMarker(SpawnPointMarker marker)
        {
            if (marker == null) return;

            if (_markers.ContainsKey(marker.SpawnIndex))
            {
                Debug.LogWarning(
                    $"[ArenaManager] Duplicate SpawnPointMarker for index {marker.SpawnIndex}. " +
                    $"Overwriting previous registration.", marker);
            }

            _markers[marker.SpawnIndex] = marker;
        }

        /// <summary>Called automatically by <see cref="SpawnPointMarker.OnDisable"/>.</summary>
        public void UnregisterMarker(SpawnPointMarker marker)
        {
            if (marker == null) return;

            // Only remove if this marker is the current registrant for that index.
            if (_markers.TryGetValue(marker.SpawnIndex, out SpawnPointMarker current) &&
                current == marker)
            {
                _markers.Remove(marker.SpawnIndex);
            }
        }

        // ── Event entry points ────────────────────────────────────────────────

        /// <summary>
        /// Called when the MatchStarted VoidGameEvent fires.
        /// Wire via a <see cref="VoidGameEventListener"/> MB on this GameObject.
        ///
        /// Validates config, then raises <c>_arenaReadyChannel</c> to signal
        /// that spawn poses are available.
        /// </summary>
        public void OnMatchStarted()
        {
            if (_arenaConfig == null)
            {
                Debug.LogError("[ArenaManager] OnMatchStarted fired but no ArenaConfig is assigned.", this);
                return;
            }

            if (!_arenaConfig.Validate(out string err))
            {
                Debug.LogError($"[ArenaManager] Invalid ArenaConfig on match start: {err}", this);
                return;
            }

            _arenaReadyChannel?.Raise();
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ArenaManager] Duplicate instance destroyed.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ValidateConfig();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ── Internal ──────────────────────────────────────────────────────────

        private void ValidateConfig()
        {
            if (_arenaConfig == null)
            {
                Debug.LogWarning("[ArenaManager] No ArenaConfig assigned. " +
                                 "Spawn poses will return Vector3.zero.", this);
                return;
            }

            if (!_arenaConfig.Validate(out string err))
                Debug.LogWarning($"[ArenaManager] ArenaConfig validation failed: {err}", this);
        }

        // ── Editor Gizmos ─────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_arenaConfig == null) return;

            // Draw arena floor footprint
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.25f);
            Vector3 center = transform.position + Vector3.up * (_arenaConfig.WallHeight * 0.5f);
            Vector3 size   = new Vector3(_arenaConfig.GroundWidth,
                                         _arenaConfig.WallHeight,
                                         _arenaConfig.GroundDepth);
            Gizmos.DrawCube(center, size);

            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.6f);
            Gizmos.DrawWireCube(center, size);

            // Draw SO-baked spawn poses that have no live scene marker
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            for (int i = 0; i < _arenaConfig.SpawnPointCount; i++)
            {
                if (_markers.ContainsKey(i)) continue; // scene marker draws itself

                SpawnPointData sp = _arenaConfig.GetSpawnPoint(i);
                Gizmos.DrawWireSphere(sp.position, 0.4f);
                Gizmos.DrawLine(sp.position, sp.position + sp.Rotation * Vector3.forward * 1.2f);

                UnityEditor.Handles.color = new Color(0.3f, 0.7f, 1f, 1f);
                UnityEditor.Handles.Label(sp.position + Vector3.up * 0.6f, $"SO Spawn [{i}]");
            }
        }
#endif
    }
}

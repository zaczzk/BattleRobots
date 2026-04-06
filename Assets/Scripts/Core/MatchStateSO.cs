using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// ScriptableObject that holds the latest known match state for both combatants.
    /// Used as the shared data bus between <see cref="NetworkMatchSync"/> (writes) and
    /// game-UI / HealthSO reconciliation logic (reads).
    ///
    /// Payload layout (12 bytes, little-endian via <see cref="BitConverter"/>):
    ///   Bytes 0–3  : PlayerHp   (float)
    ///   Bytes 4–7  : OpponentHp (float)
    ///   Bytes 8–11 : ElapsedTime (float, seconds)
    ///
    /// Architecture rules:
    ///   • <see cref="Snapshot"/> writes into a caller-supplied buffer — zero heap allocation.
    ///   • <see cref="Apply"/> validates payload length before reading; ignores bad payloads.
    ///   • SO fires <see cref="_onStateApplied"/> after every successful <see cref="Apply"/>.
    ///   • Never modified from FixedUpdate by this class — mutated only by Apply/Reset.
    ///
    /// Create via Assets ▶ BattleRobots ▶ Network ▶ MatchStateSO.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Network/MatchStateSO", order = 0)]
    public sealed class MatchStateSO : ScriptableObject
    {
        // ── Constants ─────────────────────────────────────────────────────────

        /// <summary>Expected byte-length of a serialised match-state payload.</summary>
        public const int PayloadSize = 12; // 3 floats × 4 bytes

        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels")]
        [Tooltip("Fired after every successful Apply(). Payload = the raw bytes applied.")]
        [SerializeField] private ByteArrayGameEvent _onStateApplied;

        // ── Runtime state ─────────────────────────────────────────────────────

        /// <summary>Current hit-points of the local player robot.</summary>
        public float PlayerHp { get; private set; }

        /// <summary>Current hit-points of the remote opponent robot.</summary>
        public float OpponentHp { get; private set; }

        /// <summary>Elapsed match time in seconds at the last sync point.</summary>
        public float ElapsedTime { get; private set; }

        // ── Mutators ──────────────────────────────────────────────────────────

        /// <summary>
        /// Overwrites the runtime state from a 12-byte network payload.
        /// Ignores payloads whose length does not equal <see cref="PayloadSize"/>.
        /// Fires <see cref="_onStateApplied"/> on success.
        /// </summary>
        public void Apply(byte[] payload)
        {
            if (payload == null || payload.Length != PayloadSize)
            {
                Debug.LogWarning($"[MatchStateSO] Apply: invalid payload " +
                                 $"(length={payload?.Length.ToString() ?? "null"}, " +
                                 $"expected {PayloadSize}). Ignoring.");
                return;
            }

            PlayerHp    = BitConverter.ToSingle(payload, 0);
            OpponentHp  = BitConverter.ToSingle(payload, 4);
            ElapsedTime = BitConverter.ToSingle(payload, 8);

            _onStateApplied?.Raise(payload);
        }

        /// <summary>
        /// Serialises the current state into <paramref name="buffer"/> in-place.
        /// The caller must supply a buffer of exactly <see cref="PayloadSize"/> bytes.
        /// No heap allocation — safe to call from FixedUpdate via the owning
        /// <see cref="NetworkMatchSync"/>.
        /// </summary>
        /// <param name="buffer">Pre-allocated 12-byte buffer to write into.</param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="buffer"/> is null or its length ≠ <see cref="PayloadSize"/>.
        /// </exception>
        public void Snapshot(byte[] buffer)
        {
            if (buffer == null || buffer.Length != PayloadSize)
                throw new ArgumentException(
                    $"[MatchStateSO] Snapshot requires a {PayloadSize}-byte buffer.", nameof(buffer));

            BitConverter.GetBytes(PlayerHp).CopyTo(buffer, 0);
            BitConverter.GetBytes(OpponentHp).CopyTo(buffer, 4);
            BitConverter.GetBytes(ElapsedTime).CopyTo(buffer, 8);
        }

        /// <summary>
        /// Manually set the local values that <see cref="Snapshot"/> will serialise.
        /// Call once per FixedUpdate tick in the owning MonoBehaviour before
        /// calling Snapshot, so the buffer always reflects live game state.
        /// </summary>
        public void SetLocalState(float playerHp, float opponentHp, float elapsedTime)
        {
            PlayerHp    = playerHp;
            OpponentHp  = opponentHp;
            ElapsedTime = elapsedTime;
        }

        /// <summary>
        /// Resets all runtime values to zero. Call at match end / on domain reload.
        /// Does NOT fire the event channel.
        /// </summary>
        public void Reset()
        {
            PlayerHp    = 0f;
            OpponentHp  = 0f;
            ElapsedTime = 0f;
        }
    }
}

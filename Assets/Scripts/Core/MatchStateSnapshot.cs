using System;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable snapshot of match state at a single point in time.
    /// Used as the element type of <see cref="ReplaySO"/>'s ring buffer.
    ///
    /// ARCHITECTURE:
    ///   • Plain <c>[Serializable]</c> struct — no <c>UnityEngine.Object</c> references.
    ///     Copying it into the ring-buffer array causes zero heap allocation.
    ///   • Compatible with <c>JsonUtility</c> for optional replay export.
    ///   • Vector3 fields are safe to serialise by JsonUtility (3 floats each).
    ///
    /// Populated each FixedUpdate tick by the component that drives recording
    /// (e.g. MatchManager or a dedicated RecordingDriver MB).
    /// </summary>
    [Serializable]
    public struct MatchStateSnapshot
    {
        /// <summary>Elapsed time in seconds since the match started.</summary>
        public float elapsedTime;

        /// <summary>Player 1 current hit-points at this moment.</summary>
        public float p1Hp;

        /// <summary>Player 2 current hit-points at this moment.</summary>
        public float p2Hp;

        /// <summary>Player 1 world-space position at this moment.</summary>
        public Vector3 p1Pos;

        /// <summary>Player 2 world-space position at this moment.</summary>
        public Vector3 p2Pos;

        /// <param name="elapsedTime">Seconds since match start.</param>
        /// <param name="p1Hp">Player 1 hit-points.</param>
        /// <param name="p2Hp">Player 2 hit-points.</param>
        /// <param name="p1Pos">Player 1 world position.</param>
        /// <param name="p2Pos">Player 2 world position.</param>
        public MatchStateSnapshot(float elapsedTime, float p1Hp, float p2Hp,
                                   Vector3 p1Pos, Vector3 p2Pos)
        {
            this.elapsedTime = elapsedTime;
            this.p1Hp        = p1Hp;
            this.p2Hp        = p2Hp;
            this.p1Pos       = p1Pos;
            this.p2Pos       = p2Pos;
        }

        public override string ToString() =>
            $"MatchStateSnapshot(t={elapsedTime:F2}s, p1={p1Hp:F0}hp@{p1Pos}, p2={p2Hp:F0}hp@{p2Pos})";
    }
}

using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Configuration ScriptableObject for the post-respawn invulnerability window.
    ///
    /// Holds two read-only settings consumed by
    /// <see cref="BattleRobots.Physics.RespawnProtectionController"/>:
    ///   • <see cref="ProtectionDuration"/> — how long the invulnerability window lasts.
    ///   • <see cref="FullArmorRating"/>    — the armor value applied during protection
    ///                                        (100 = fully immune to all damage).
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.Core namespace; no Physics / UI references.
    ///   - SO is immutable at runtime — all mutable state (timer, protection flag,
    ///     saved armor) lives in the Physics-layer controller.
    ///   - <see cref="FullArmorRating"/> is clamped [0, 100] to match
    ///     <see cref="BattleRobots.Physics.DamageReceiver.SetArmorRating"/>'s contract.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ RespawnProtection.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/RespawnProtection")]
    public sealed class RespawnProtectionSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Protection Settings")]
        [Tooltip("Seconds of invulnerability granted after each respawn. " +
                 "Keep this short enough that it does not unbalance combat.")]
        [SerializeField, Min(0.1f)] private float _protectionDuration = 3f;

        [Tooltip("Armor rating applied to the DamageReceiver during the protection window. " +
                 "100 = fully immune to damage; 0 = no extra protection. Clamped [0, 100].")]
        [SerializeField, Range(0, 100)] private int _fullArmorRating = 100;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Seconds of invulnerability granted after each respawn.</summary>
        public float ProtectionDuration => _protectionDuration;

        /// <summary>
        /// Armor rating applied during the protection window.
        /// Clamped to [0, 100] to match DamageReceiver.SetArmorRating's contract.
        /// </summary>
        public int FullArmorRating => _fullArmorRating;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_protectionDuration < 0.5f)
                Debug.LogWarning($"[RespawnProtectionSO] '{name}': _protectionDuration ({_protectionDuration:F2}s) " +
                                 "is very short — players may not notice the invulnerability window.");
        }
#endif
    }
}

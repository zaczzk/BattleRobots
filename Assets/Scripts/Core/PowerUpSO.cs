using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Enumerates the types of instant effect a power-up pickup can apply.
    /// </summary>
    public enum PowerUpType
    {
        /// <summary>Restores a flat amount of health to the collecting robot.</summary>
        HealthRestore,

        /// <summary>Instantly recharges a flat amount of the collecting robot's shield HP.</summary>
        ShieldRecharge
    }

    /// <summary>
    /// Immutable configuration SO that describes a single power-up pickup variant.
    ///
    /// ── Design ───────────────────────────────────────────────────────────────────
    ///   • <see cref="PowerUpType"/> determines which stat is restored on pickup.
    ///   • <see cref="EffectAmount"/> is the flat HP/shield units restored (clamped ≥ 0).
    ///   • <see cref="DisplayName"/> is an optional human-readable label for UI use.
    ///   • <see cref="FirePickedUp"/> wraps the optional <see cref="VoidGameEvent"/>
    ///     so callers do not need to null-check the channel directly.
    ///
    /// ── Architecture ─────────────────────────────────────────────────────────────
    ///   BattleRobots.Core namespace. No Physics or UI references.
    ///   Immutable at runtime — all mutation is through the applying MB
    ///   (<see cref="BattleRobots.Physics.PowerUpController"/>).
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Arena ▶ PowerUp.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Arena/PowerUp", order = 30)]
    public sealed class PowerUpSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Power-Up Type")]
        [Tooltip("Determines which stat is restored when a robot picks up this power-up.")]
        [SerializeField] private PowerUpType _type = PowerUpType.HealthRestore;

        [Tooltip("Flat amount of HP or shield HP restored on pickup. Clamped to ≥ 0.")]
        [SerializeField, Min(0f)] private float _effectAmount = 25f;

        [Tooltip("Optional human-readable label shown in UI (e.g. HUD notification).")]
        [SerializeField] private string _displayName = "";

        [Header("Event Channel (optional)")]
        [Tooltip("Raised each time this power-up is collected. Wire to audio or VFX.")]
        [SerializeField] private VoidGameEvent _onPickedUp;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Which stat category this power-up restores.</summary>
        public PowerUpType Type         => _type;

        /// <summary>Flat restoration amount (HP units or shield HP units). Always ≥ 0.</summary>
        public float       EffectAmount => _effectAmount;

        /// <summary>Optional label for UI display; may be empty.</summary>
        public string      DisplayName  => _displayName;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Raises the optional <see cref="_onPickedUp"/> channel.
        /// Safe to call when no channel is assigned — null is silently ignored.
        /// </summary>
        public void FirePickedUp() => _onPickedUp?.Raise();

        // ── Validation ────────────────────────────────────────────────────────

        private void OnValidate()
        {
            if (_effectAmount < 0f)
                _effectAmount = 0f;
        }
    }
}

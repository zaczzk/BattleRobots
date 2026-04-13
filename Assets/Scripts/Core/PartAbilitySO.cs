using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Data-driven definition of a robot part's special ability.
    ///
    /// Consumed by <see cref="BattleRobots.Physics.AbilityController"/> at runtime to
    /// determine the energy cost and cooldown duration for a single activatable ability.
    ///
    /// ── ARCHITECTURE RULES ───────────────────────────────────────────────────
    ///   • SO asset is immutable at runtime — all fields are read-only properties.
    ///   • Gameplay logic lives in AbilityController, not here.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Physics/PartAbility")]
    public sealed class PartAbilitySO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Unique string identifier for this ability (used in logs and save data).")]
        [SerializeField] private string _abilityId;

        [Tooltip("Human-readable name shown in the HUD.")]
        [SerializeField] private string _abilityName;

        [Tooltip("Energy consumed from EnergySystemSO on each successful activation.")]
        [SerializeField, Min(0f)] private float _energyCost = 10f;

        [Tooltip("Seconds the ability is unavailable after activation.")]
        [SerializeField, Min(0f)] private float _cooldown = 5f;

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Unique string identifier for this ability.</summary>
        public string AbilityId => _abilityId;

        /// <summary>Human-readable name shown in the HUD.</summary>
        public string AbilityName => _abilityName;

        /// <summary>Energy cost per activation. Always ≥ 0.</summary>
        public float EnergyCost => _energyCost;

        /// <summary>Post-activation cooldown in seconds. Always ≥ 0.</summary>
        public float CooldownDuration => _cooldown;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_abilityId))
                Debug.LogWarning(
                    $"[PartAbilitySO] '{name}' has an empty _abilityId. " +
                    "Set a unique id to avoid log and save-data collisions.");
        }
#endif
    }
}

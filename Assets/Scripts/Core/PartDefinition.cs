using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable ScriptableObject describing a purchasable robot part.
    ///
    /// Lives in BattleRobots.Core (pure data SO — no UI or Physics references).
    /// Create via Assets ▶ BattleRobots ▶ Shop ▶ PartDefinition.
    ///
    /// Runtime state (owned / equipped) lives elsewhere; this SO is never
    /// mutated at runtime in accordance with the SO-immutability architecture rule.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Shop/PartDefinition", order = 0)]
    public sealed class PartDefinition : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Human-readable name displayed in the shop UI.")]
        [SerializeField] private string _partName = "Unnamed Part";

        [Tooltip("Short description shown in the shop browser.")]
        [TextArea(2, 4)]
        [SerializeField] private string _description = "";

        [Tooltip("Thumbnail sprite shown in the shop browser.")]
        [SerializeField] private Sprite _thumbnail;

        // ── Classification ────────────────────────────────────────────────────

        [Header("Classification")]
        [Tooltip("Which slot category this part occupies on a robot chassis.")]
        [SerializeField] private PartCategory _category = PartCategory.Weapon;

        // ── Economy ───────────────────────────────────────────────────────────

        [Header("Economy")]
        [Tooltip("Purchase price in in-game currency.")]
        [SerializeField, Min(0)] private int _cost = 100;

        // ── Stats ─────────────────────────────────────────────────────────────

        [Header("Stats (optional modifiers applied when equipped)")]
        [Tooltip("Additive hit-point modifier when this part is equipped.")]
        [SerializeField] private float _hpModifier = 0f;

        [Tooltip("Additive damage modifier when this part is equipped.")]
        [SerializeField] private float _damageModifier = 0f;

        [Tooltip("Additive speed modifier when this part is equipped.")]
        [SerializeField] private float _speedModifier = 0f;

        // ── Public API ────────────────────────────────────────────────────────

        public string       PartName       => _partName;
        public string       Description    => _description;
        public Sprite       Thumbnail      => _thumbnail;
        public PartCategory Category       => _category;
        public int          Cost           => _cost;
        public float        HpModifier     => _hpModifier;
        public float        DamageModifier => _damageModifier;
        public float        SpeedModifier  => _speedModifier;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_partName))
                Debug.LogWarning($"[PartDefinition] '{name}': PartName must not be empty.");
            if (_cost < 0)
                Debug.LogWarning($"[PartDefinition] '{name}': Cost must be non-negative.");
        }
#endif
    }
}

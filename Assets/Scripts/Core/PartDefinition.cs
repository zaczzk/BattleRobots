using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// ScriptableObject representing a single purchaseable robot part in the shop.
    ///
    /// Instances live in Assets/ScriptableObjects/Parts/.
    /// Immutable at runtime — ShopUI reads stats; PlayerWallet.Deduct is called on purchase.
    ///
    /// Create via:  Assets ▶ Create ▶ BattleRobots ▶ Shop ▶ PartDefinition
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Shop/PartDefinition", order = 0)]
    public sealed class PartDefinition : ScriptableObject
    {
        // ── Identity ──────────────────────────────────────────────────────────

        [Header("Identity")]
        [Tooltip("Unique string key used to track ownership in save data. " +
                 "Must match the SlotId value in any RobotDefinition that uses this part.")]
        [SerializeField] private string _partId;

        [Tooltip("Display name shown in the shop listing.")]
        [SerializeField] private string _displayName = "Unnamed Part";

        [Tooltip("Two-line description shown in the part details panel.")]
        [TextArea(2, 3)]
        [SerializeField] private string _description = string.Empty;

        [Tooltip("Thumbnail shown in the part browser grid.")]
        [SerializeField] private Sprite _thumbnail;

        // ── Stats ─────────────────────────────────────────────────────────────

        [Header("Stats")]
        [Tooltip("Which slot type this part occupies on a robot chassis.")]
        [SerializeField] private PartSlotType _slotType = PartSlotType.Weapon;

        [Tooltip("Shop purchase price in currency units.")]
        [SerializeField, Min(0)] private int _cost = 150;

        [Tooltip("HP bonus granted to the robot when this part is equipped.")]
        [SerializeField, Min(0f)] private float _hpBonus = 0f;

        [Tooltip("Speed multiplier bonus (additive). 0 = no change.")]
        [SerializeField, Min(0f)] private float _speedBonus = 0f;

        [Tooltip("Torque multiplier bonus (additive). 0 = no change.")]
        [SerializeField, Min(0f)] private float _torqueBonus = 0f;

        // ── Public API ────────────────────────────────────────────────────────

        public string      PartId      => _partId;
        public string      DisplayName => _displayName;
        public string      Description => _description;
        public Sprite      Thumbnail   => _thumbnail;
        public PartSlotType SlotType   => _slotType;
        public int         Cost        => _cost;
        public float       HpBonus     => _hpBonus;
        public float       SpeedBonus  => _speedBonus;
        public float       TorqueBonus => _torqueBonus;
    }
}

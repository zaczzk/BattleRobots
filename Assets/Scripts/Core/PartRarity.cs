namespace BattleRobots.Core
{
    /// <summary>
    /// Rarity tier of a robot part, ordered from most-common to most-rare.
    ///
    /// Used by <see cref="PartRarityConfig"/> to map each tier to a display name,
    /// UI tint colour, and loot-drop weight multiplier.  Stored on
    /// <see cref="PartDefinition"/> as an inspector-configurable enum field.
    /// </summary>
    public enum PartRarity
    {
        Common    = 0,
        Uncommon  = 1,
        Rare      = 2,
        Epic      = 3,
        Legendary = 4,
    }
}

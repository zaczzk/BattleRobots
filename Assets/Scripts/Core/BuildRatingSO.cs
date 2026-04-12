using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Runtime ScriptableObject blackboard that stores the player's current
    /// "build power" rating and exposes it to UI and other systems.
    ///
    /// ── Mutators ──────────────────────────────────────────────────────────────
    ///   • <see cref="UpdateRating"/> — re-evaluates the score via
    ///     <see cref="RobotBuildRatingCalculator.Calculate"/>, stores the new value,
    ///     and fires <c>_onRatingChanged</c> with the int result.
    ///     Null-safe: all parameters may be null (produces 0 when config is null).
    ///   • <see cref="Reset"/>        — silently sets CurrentRating to 0 (no event).
    ///
    /// ── Architecture ──────────────────────────────────────────────────────────
    ///   BattleRobots.Core namespace; no Physics / UI references.
    ///   The SO is mutated only through <see cref="UpdateRating"/> (called by
    ///   <see cref="BattleRobots.UI.BuildRatingController"/>).
    ///   <c>_onRatingChanged</c> is optional and null-safe.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ BuildRating.
    /// </summary>
    [CreateAssetMenu(
        menuName = "BattleRobots/Core/BuildRating",
        fileName = "BuildRatingSO")]
    public sealed class BuildRatingSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels — Out")]
        [Tooltip("Fired every time UpdateRating() recalculates the score. "
               + "Carries the new int rating value. Leave null if unused.")]
        [SerializeField] private IntGameEvent _onRatingChanged;

        // ── Runtime state ─────────────────────────────────────────────────────

        private int _currentRating;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// The most recently calculated build power rating.
        /// 0 on a fresh SO and after <see cref="Reset"/>.
        /// </summary>
        public int CurrentRating => _currentRating;

        /// <summary>
        /// Recalculates the build power rating from the supplied inputs and stores
        /// the result.  Fires <c>_onRatingChanged</c> with the new value.
        ///
        /// All parameters are optional — null arguments are forwarded safely to
        /// <see cref="RobotBuildRatingCalculator.Calculate"/> which returns 0 when
        /// essential inputs (loadout, catalog, or config) are missing.
        /// </summary>
        public void UpdateRating(
            PlayerLoadout          loadout,
            ShopCatalog            catalog,
            PlayerPartUpgrades     upgrades,
            PartSynergyConfig      synergyConfig,
            RobotBuildRatingConfig ratingConfig)
        {
            _currentRating = RobotBuildRatingCalculator.Calculate(
                loadout, catalog, upgrades, synergyConfig, ratingConfig);

            _onRatingChanged?.Raise(_currentRating);
        }

        /// <summary>
        /// Silently resets the current rating to 0 without firing any event.
        /// </summary>
        public void Reset()
        {
            _currentRating = 0;
        }
    }
}

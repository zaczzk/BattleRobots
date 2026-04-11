using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Immutable SO listing all selectable opponents available in the game.
    ///
    /// ── Usage ─────────────────────────────────────────────────────────────────
    ///   Assign this SO to <see cref="BattleRobots.UI.OpponentSelectionController"/>.
    ///   The controller cycles through <see cref="Opponents"/> and writes the chosen
    ///   <see cref="OpponentProfileSO"/> to <see cref="SelectedOpponentSO"/>.
    ///   <see cref="BattleRobots.Physics.RobotAIController"/> reads the active SO
    ///   at Awake time to apply the profile's difficulty and personality overrides.
    ///
    /// Create via Assets ▶ Create ▶ BattleRobots ▶ Core ▶ OpponentRoster.
    /// Suggested starting roster: Easy Bot, Normal Bot, Hard Bot, Berserker, Tactician.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Core/OpponentRoster", order = 11)]
    public sealed class OpponentRosterSO : ScriptableObject
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Ordered list of opponent profiles available for player selection. " +
                 "Index 0 is the default selection shown when the panel opens.")]
        [SerializeField] private List<OpponentProfileSO> _opponents = new List<OpponentProfileSO>();

        // ── Public API (immutable at runtime) ─────────────────────────────────

        /// <summary>
        /// Ordered, read-only list of opponent profiles.
        /// Index 0 is shown as the default selection.
        /// </summary>
        public IReadOnlyList<OpponentProfileSO> Opponents => _opponents;

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_opponents == null || _opponents.Count == 0)
            {
                Debug.LogWarning($"[OpponentRosterSO] '{name}': No opponents defined. " +
                                 "Add at least one OpponentProfileSO entry.");
                return;
            }

            for (int i = 0; i < _opponents.Count; i++)
            {
                if (_opponents[i] == null)
                    Debug.LogWarning($"[OpponentRosterSO] '{name}': " +
                                     $"Opponent at index {i} is null — remove or replace it.");
            }
        }
#endif
    }
}

using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Drives a single row in the achievements panel UI.
    ///
    /// Each row displays one <see cref="AchievementDefinitionSO"/>:
    /// name, description, unlock status (UNLOCKED / current progress), and
    /// optional credit reward.
    ///
    /// Populated by <see cref="AchievementsUIController.PopulateCatalog"/>.
    /// All text references are optional — omit any field you don't need in the prefab.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — no Physics refs.
    ///   • <see cref="Setup"/> is called once per row; no Update overhead.
    ///   • Static formatting helper <see cref="FormatProgress"/> is private static
    ///     so it can be verified via reflection in EditMode tests without scene setup.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AchievementRowController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Tooltip("Displays the achievement display name, e.g. 'First Victory'.")]
        [SerializeField] private Text _nameText;

        [Tooltip("Displays the one-sentence unlock-condition description.")]
        [SerializeField] private Text _descriptionText;

        [Tooltip("Displays 'UNLOCKED' when earned, or 'N / M' progress otherwise.")]
        [SerializeField] private Text _statusText;

        [Tooltip("Displays the credit reward as '+N'. Hidden (empty string) when reward is 0.")]
        [SerializeField] private Text _rewardText;

        [Tooltip("Optional badge GameObject shown only when the achievement is unlocked. " +
                 "Activate/deactivate driven by isUnlocked.")]
        [SerializeField] private GameObject _unlockedBadge;

        // ── Static labels (avoids per-row string allocation) ──────────────────

        private const string UnlockedLabel = "UNLOCKED";

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Populate all UI fields from the supplied definition and current runtime state.
        ///
        /// Call immediately after <see cref="Instantiate"/> before the row is first
        /// rendered; subsequent per-frame updates are not supported (one definition → one row).
        ///
        /// <paramref name="currentProgress"/> is the relevant runtime counter for this
        /// achievement's <see cref="AchievementTrigger"/> type (e.g. total wins, current
        /// level). Determined by <see cref="AchievementsUIController.PopulateCatalog"/>.
        /// </summary>
        /// <param name="def">Immutable achievement definition.  Null returns early.</param>
        /// <param name="isUnlocked">Whether the player has already earned this achievement.</param>
        /// <param name="currentProgress">
        ///   Current value of the relevant trigger counter.
        ///   Displayed as "currentProgress / TargetCount" when not yet unlocked.
        /// </param>
        public void Setup(AchievementDefinitionSO def, bool isUnlocked, int currentProgress)
        {
            if (def == null) return;

            if (_nameText != null)
                _nameText.text = def.DisplayName;

            if (_descriptionText != null)
                _descriptionText.text = def.Description;

            if (_statusText != null)
                _statusText.text = isUnlocked
                    ? UnlockedLabel
                    : FormatProgress(currentProgress, def.TargetCount);

            if (_rewardText != null)
                _rewardText.text = def.RewardCredits > 0
                    ? "+" + def.RewardCredits
                    : string.Empty;

            if (_unlockedBadge != null)
                _unlockedBadge.SetActive(isUnlocked);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Formats a progress fraction as "current / target", e.g. "3 / 5".
        /// Private static so it can be exercised headlessly via reflection in EditMode tests.
        /// </summary>
        private static string FormatProgress(int current, int target)
            => current + " / " + target;
    }
}

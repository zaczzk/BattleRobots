using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that shows an "Achievement Unlocked!" banner whenever
    /// <see cref="PlayerAchievementsSO"/> raises <c>_onAchievementUnlocked</c>.
    ///
    /// ── Notification flow ────────────────────────────────────────────────────────
    ///   1. <c>_onAchievementUnlocked</c> fires.
    ///   2. Read <see cref="PlayerAchievementsSO.LastUnlockedId"/>.
    ///   3. Look up the matching <see cref="AchievementDefinitionSO"/> in the catalog.
    ///   4. Set <c>_achievementNameLabel</c> to the definition's DisplayName.
    ///   5. Set <c>_rewardLabel</c> to "+N credits" (hidden when reward is 0).
    ///   6. Activate <c>_notificationPanel</c> and start the display timer.
    ///   7. <see cref="Tick"/> decrements the timer each frame; hides the panel when it expires.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one notification banner per canvas.
    ///   - All inspector fields are optional — assign only those present in the scene.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _playerAchievements    → shared PlayerAchievementsSO runtime SO.
    ///   _catalog               → shared AchievementCatalogSO asset.
    ///   _onAchievementUnlocked → same VoidGameEvent as PlayerAchievementsSO._onAchievementUnlocked.
    ///   _notificationPanel     → banner root panel (starts inactive).
    ///   _achievementNameLabel  → Text child for the achievement display name.
    ///   _rewardLabel           → Text child for "+N credits" (optional).
    ///   _displayDuration       → seconds the banner stays visible (default 3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AchievementUnlockNotificationController : MonoBehaviour
    {
        // ── Inspector — Data ─────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("Runtime SO tracking unlocked achievements. LastUnlockedId is read on notification.")]
        [SerializeField] private PlayerAchievementsSO _playerAchievements;

        [Tooltip("Catalog SO — used to look up the definition for LastUnlockedId.")]
        [SerializeField] private AchievementCatalogSO _catalog;

        // ── Inspector — Event ────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised by PlayerAchievementsSO when any achievement is first unlocked.")]
        [SerializeField] private VoidGameEvent _onAchievementUnlocked;

        // ── Inspector — UI ───────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Root banner panel (starts inactive).")]
        [SerializeField] private GameObject _notificationPanel;

        [Tooltip("Text label receiving the achievement display name.")]
        [SerializeField] private Text _achievementNameLabel;

        [Tooltip("Text label receiving '+N credits'. Left empty when reward is 0.")]
        [SerializeField] private Text _rewardLabel;

        [Tooltip("Seconds the banner stays visible before auto-hiding.")]
        [SerializeField, Min(0.1f)] private float _displayDuration = 3f;

        // ── Runtime ──────────────────────────────────────────────────────────

        private float  _displayTimer;
        private Action _onUnlockedDelegate;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _onUnlockedDelegate = OnAchievementUnlocked;
        }

        private void OnEnable()
        {
            _onAchievementUnlocked?.RegisterCallback(_onUnlockedDelegate);
            _notificationPanel?.SetActive(false);
            _displayTimer = 0f;
        }

        private void OnDisable()
        {
            _onAchievementUnlocked?.UnregisterCallback(_onUnlockedDelegate);
            _notificationPanel?.SetActive(false);
            _displayTimer = 0f;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Logic ─────────────────────────────────────────────────────────────

        private void OnAchievementUnlocked()
        {
            if (_playerAchievements == null) return;

            string id = _playerAchievements.LastUnlockedId;
            if (string.IsNullOrEmpty(id)) return;

            AchievementDefinitionSO def = FindDefinition(id);
            ShowNotification(def);
        }

        private AchievementDefinitionSO FindDefinition(string id)
        {
            if (_catalog == null) return null;
            var list = _catalog.Achievements;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null && list[i].Id == id)
                    return list[i];
            }
            return null;
        }

        /// <summary>
        /// Activates the notification panel with the given definition's name and reward.
        /// Null definition results in empty labels (banner still shown).
        /// Resets the display timer to <see cref="DisplayDuration"/>.
        /// </summary>
        internal void ShowNotification(AchievementDefinitionSO def)
        {
            if (_achievementNameLabel != null)
                _achievementNameLabel.text = def != null ? def.DisplayName : string.Empty;

            if (_rewardLabel != null)
                _rewardLabel.text = def != null && def.RewardCredits > 0
                    ? string.Format("+{0} credits", def.RewardCredits)
                    : string.Empty;

            _notificationPanel?.SetActive(true);
            _displayTimer = _displayDuration;
        }

        /// <summary>
        /// Advances the display timer by <paramref name="dt"/> seconds.
        /// Hides the panel when the timer reaches zero.
        /// No-op when the timer is already at zero.
        /// </summary>
        public void Tick(float dt)
        {
            if (_displayTimer <= 0f) return;
            _displayTimer -= dt;
            if (_displayTimer <= 0f)
                _notificationPanel?.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>The configured display duration in seconds (Inspector field).</summary>
        public float DisplayDuration => _displayDuration;

        /// <summary>Seconds remaining before the notification auto-hides. Zero when hidden.</summary>
        public float DisplayTimer => _displayTimer;

        /// <summary>The assigned <see cref="PlayerAchievementsSO"/>. May be null.</summary>
        public PlayerAchievementsSO PlayerAchievements => _playerAchievements;

        /// <summary>The assigned <see cref="AchievementCatalogSO"/>. May be null.</summary>
        public AchievementCatalogSO Catalog => _catalog;
    }
}

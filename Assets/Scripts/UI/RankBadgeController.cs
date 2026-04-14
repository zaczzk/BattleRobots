using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the player's current prestige rank badge using a data-driven
    /// <see cref="RankBadgeConfig"/> sprite mapping.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   • On <c>OnEnable</c> and each time <c>_onPrestige</c> fires, <see cref="Refresh"/>
    ///     is called.
    ///   • Refresh reads <see cref="PrestigeSystemSO.GetRankLabel"/> and calls
    ///     <see cref="RankBadgeConfig.GetBadge"/> to resolve the sprite.
    ///   • The badge <see cref="Image"/> sprite and optional rank label Text are updated.
    ///
    /// ── Architecture notes ───────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - DisallowMultipleComponent — one rank badge per canvas.
    ///   - All inspector fields optional — assign only those present in the scene.
    ///   - Delegate cached in Awake; zero heap allocations after initialisation.
    ///
    /// Scene wiring:
    ///   _prestigeSystem  → shared PrestigeSystemSO asset.
    ///   _badgeConfig     → shared RankBadgeConfig SO (sprite map).
    ///   _onPrestige      → same VoidGameEvent raised by PrestigeSystemSO.Prestige().
    ///   _badgeImage      → Image component that receives the badge sprite.
    ///   _rankLabel       → Text showing the rank label (e.g. "Silver II").
    ///   _panel           → optional container shown when the badge is valid.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RankBadgeController : MonoBehaviour
    {
        // ── Inspector — Data ─────────────────────────────────────────────────

        [Header("Data (optional)")]
        [Tooltip("PrestigeSystemSO providing the current rank label.")]
        [SerializeField] private PrestigeSystemSO _prestigeSystem;

        [Tooltip("RankBadgeConfig mapping rank labels to badge sprites.")]
        [SerializeField] private RankBadgeConfig _badgeConfig;

        // ── Inspector — Event ────────────────────────────────────────────────

        [Header("Event Channel — In (optional)")]
        [Tooltip("Raised by PrestigeSystemSO each time the player prestiges.")]
        [SerializeField] private VoidGameEvent _onPrestige;

        // ── Inspector — UI ───────────────────────────────────────────────────

        [Header("UI (all optional)")]
        [Tooltip("Image component that displays the badge sprite.")]
        [SerializeField] private Image _badgeImage;

        [Tooltip("Text component showing the rank label.")]
        [SerializeField] private Text _rankLabel;

        [Tooltip("Optional root panel — activated on Refresh.")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegate ──────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPrestige?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPrestige?.UnregisterCallback(_refreshDelegate);
        }

        // ── Logic ────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads the current rank label from <see cref="PrestigeSystemSO"/> and
        /// applies the matching sprite and text to the UI.
        /// Safe to call with any combination of null references.
        /// </summary>
        public void Refresh()
        {
            string label = _prestigeSystem?.GetRankLabel() ?? "None";

            if (_rankLabel != null)
                _rankLabel.text = label;

            if (_badgeImage != null && _badgeConfig != null)
            {
                Sprite badge = _badgeConfig.GetBadge(label);
                if (badge != null)
                    _badgeImage.sprite = badge;
            }

            _panel?.SetActive(true);
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="PrestigeSystemSO"/>. May be null.</summary>
        public PrestigeSystemSO PrestigeSystem => _prestigeSystem;

        /// <summary>The assigned <see cref="RankBadgeConfig"/>. May be null.</summary>
        public RankBadgeConfig BadgeConfig => _badgeConfig;
    }
}

using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that visualises zone ownership from
    /// <see cref="ZoneControlZoneControllerCatalogSO"/> as a coloured badge grid.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _zoneBadgeImages  → Image per zone; coloured _playerColor or _botColor.
    ///   _summaryLabel     → "Player Zones: N / Total".
    ///   _panel            → Root panel; hidden when _catalogSO is null.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   Subscribes <c>_onControlChanged</c> → <see cref="Refresh"/>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegate cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one ownership map HUD per canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlOwnershipMapHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlZoneControllerCatalogSO _catalogSO;

        [Header("Event Channels — In (optional)")]
        [Tooltip("Wire to ZoneControlZoneControllerCatalogSO._onControlChanged.")]
        [SerializeField] private VoidGameEvent _onControlChanged;

        [Header("UI Refs (optional)")]
        [Tooltip("One Image per zone; colored by ownership.")]
        [SerializeField] private Image[] _zoneBadgeImages;
        [SerializeField] private Text    _summaryLabel;
        [SerializeField] private Color   _playerColor = Color.blue;
        [SerializeField] private Color   _botColor    = Color.red;

        [Header("UI Refs — Panel (optional)")]
        [SerializeField] private GameObject _panel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _refreshDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake() => _refreshDelegate = Refresh;

        private void OnEnable()
        {
            _onControlChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onControlChanged?.UnregisterCallback(_refreshDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the badge grid and summary label from the current catalog state.
        /// Hides the panel when <c>_catalogSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_catalogSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_zoneBadgeImages != null)
            {
                for (int i = 0; i < _zoneBadgeImages.Length; i++)
                {
                    if (_zoneBadgeImages[i] == null) continue;
                    bool playerOwns = i < _catalogSO.ZoneCount && _catalogSO.GetZoneController(i);
                    _zoneBadgeImages[i].color = playerOwns ? _playerColor : _botColor;
                }
            }

            if (_summaryLabel != null)
                _summaryLabel.text =
                    $"Player Zones: {_catalogSO.PlayerOwnedCount} / {_catalogSO.ZoneCount}";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound catalog SO (may be null).</summary>
        public ZoneControlZoneControllerCatalogSO CatalogSO => _catalogSO;
    }
}

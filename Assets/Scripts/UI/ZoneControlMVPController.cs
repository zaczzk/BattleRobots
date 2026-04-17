using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays the match MVP result after <c>_onMatchEnded</c>
    /// fires, reading <see cref="ZoneControlMVPSO.IsPlayerMVP"/> to set the label.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On <c>_onMatchEnded</c>: calls <see cref="Refresh"/> then raises
    ///   <c>_onMVPDetermined</c> (if the SO is wired).
    ///   <see cref="Refresh"/>: hides the panel when the SO is null; otherwise shows
    ///   it and sets <c>_mvpLabel</c> to <c>"MVP: Player"</c> or <c>"MVP: Bot"</c>.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegate cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one MVP controller per scene.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _mvpSO        → ZoneControlMVPSO asset.
    ///   2. Assign _onMatchEnded → shared MatchEnded VoidGameEvent.
    ///   3. Assign _mvpLabel     → Text component for the MVP name.
    ///   4. Assign _mvpPanel     → panel root GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMVPController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMVPSO _mvpSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;

        [Header("Event Channels — Out (optional)")]
        [SerializeField] private VoidGameEvent _onMVPDetermined;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _mvpLabel;
        [SerializeField] private GameObject _mvpPanel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake() => _handleMatchEndedDelegate = HandleMatchEnded;

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Refreshes the MVP display and raises <c>_onMVPDetermined</c> when the SO
        /// is wired.  Called by <c>_onMatchEnded</c>.
        /// </summary>
        public void HandleMatchEnded()
        {
            Refresh();
            if (_mvpSO != null)
                _onMVPDetermined?.Raise();
        }

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>
        /// Updates the MVP label and panel visibility.
        /// Hides the panel when <c>_mvpSO</c> is null.
        /// </summary>
        public void Refresh()
        {
            if (_mvpSO == null)
            {
                _mvpPanel?.SetActive(false);
                return;
            }

            _mvpPanel?.SetActive(true);

            if (_mvpLabel != null)
                _mvpLabel.text = _mvpSO.IsPlayerMVP ? "MVP: Player" : "MVP: Bot";
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound MVP SO (may be null).</summary>
        public ZoneControlMVPSO MVPSO => _mvpSO;
    }
}

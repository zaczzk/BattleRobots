using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that triggers a match recap build at match end and displays
    /// the result in a HUD panel.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On <c>_onMatchEnded</c>: calls <see cref="ZoneControlMatchRecapSO.BuildRecap"/>
    ///   with the bound SOs, then calls <see cref="Refresh"/>.
    ///   On <c>_onRecapBuilt</c>: calls <see cref="Refresh"/> to update the HUD.
    ///   On <c>_onMatchStarted</c>: resets the SO and hides the panel.
    ///   <see cref="Refresh"/>: hides panel when SO is null or recap not built;
    ///   shows panel and sets the label to <c>GetRecapSummary()</c> when built.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All inspector fields optional and null-safe.
    ///   - Delegates cached in Awake; zero alloc after init.
    ///   - DisallowMultipleComponent — one recap controller per scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchRecapController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlMatchRecapSO      _recapSO;
        [SerializeField] private ZoneControlVictoryConditionSO _victorySO;
        [SerializeField] private ZoneControlMVPSO              _mvpSO;
        [SerializeField] private ZoneControlScoreboardSO       _scoreboardSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onRecapBuilt;
        [SerializeField] private VoidGameEvent _onMatchStarted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _recapLabel;
        [SerializeField] private GameObject _recapPanel;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleMatchEndedDelegate;
        private Action _handleRecapBuiltDelegate;
        private Action _handleMatchStartedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleMatchEndedDelegate   = HandleMatchEnded;
            _handleRecapBuiltDelegate   = Refresh;
            _handleMatchStartedDelegate = HandleMatchStarted;
        }

        private void OnEnable()
        {
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onRecapBuilt?.RegisterCallback(_handleRecapBuiltDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onRecapBuilt?.UnregisterCallback(_handleRecapBuiltDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        /// <summary>Builds the recap from bound SOs and refreshes the display.</summary>
        public void HandleMatchEnded()
        {
            _recapSO?.BuildRecap(_victorySO, _mvpSO, _scoreboardSO);
            Refresh();
        }

        /// <summary>Resets recap state and hides the panel at match start.</summary>
        public void HandleMatchStarted()
        {
            _recapSO?.Reset();
            Refresh();
        }

        // ── Display ───────────────────────────────────────────────────────────

        /// <summary>
        /// Updates panel visibility and label.
        /// Hides the panel when the SO is null or the recap is not yet built.
        /// </summary>
        public void Refresh()
        {
            if (_recapSO == null || !_recapSO.IsBuilt)
            {
                _recapPanel?.SetActive(false);
                return;
            }

            _recapPanel?.SetActive(true);

            if (_recapLabel != null)
                _recapLabel.text = _recapSO.GetRecapSummary();
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The bound recap SO (may be null).</summary>
        public ZoneControlMatchRecapSO RecapSO => _recapSO;
    }
}

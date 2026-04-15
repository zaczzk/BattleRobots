using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that displays the current match pace from a
    /// <see cref="MatchPaceSO"/> as a rate label, a fill bar, and timed
    /// fast/slow indicator overlays.
    ///
    /// ── Layout ─────────────────────────────────────────────────────────────────
    ///   _paceLabel      → "N.N/s"   (EventRate, one decimal place)
    ///   _paceBar        → Slider.value = Clamp01(EventRate / FastThreshold)
    ///   _fastIndicator  → activated on fast-pace event; auto-hides after duration
    ///   _slowIndicator  → activated on slow-pace event; auto-hides after duration
    ///   _panel          → activated on Refresh; hidden when SO is null
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - Subscribes to _onFastPace and _onSlowPace channels from MatchPaceSO.
    ///   - All UI refs optional; null-safe throughout.
    ///   - Delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one controller per HUD panel.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign <c>_paceSO</c>       → the MatchPaceSO asset.
    ///   2. Assign <c>_onFastPace</c>   → MatchPaceSO._onFastPace channel.
    ///   3. Assign <c>_onSlowPace</c>   → MatchPaceSO._onSlowPace channel.
    ///   4. Assign optional UI references.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MatchPaceHUDController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private MatchPaceSO _paceSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onFastPace;
        [SerializeField] private VoidGameEvent _onSlowPace;

        [Header("UI Refs (optional)")]
        [SerializeField] private Text       _paceLabel;
        [SerializeField] private Slider     _paceBar;
        [SerializeField] private GameObject _fastIndicator;
        [SerializeField] private GameObject _slowIndicator;
        [SerializeField] private GameObject _panel;

        [Header("Indicator Settings")]
        [Tooltip("Seconds the fast-pace indicator stays visible.")]
        [SerializeField, Min(0.1f)] private float _fastIndicatorDuration = 3f;

        [Tooltip("Seconds the slow-pace indicator stays visible.")]
        [SerializeField, Min(0.1f)] private float _slowIndicatorDuration = 3f;

        // ── Runtime state ─────────────────────────────────────────────────────

        private float _fastTimer;
        private float _slowTimer;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _onFastDelegate;
        private Action _onSlowDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _onFastDelegate = OnFastPace;
            _onSlowDelegate = OnSlowPace;
        }

        private void OnEnable()
        {
            _onFastPace?.RegisterCallback(_onFastDelegate);
            _onSlowPace?.RegisterCallback(_onSlowDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onFastPace?.UnregisterCallback(_onFastDelegate);
            _onSlowPace?.UnregisterCallback(_onSlowDelegate);

            _fastIndicator?.SetActive(false);
            _slowIndicator?.SetActive(false);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Advances indicator timers by <paramref name="dt"/> and hides expired ones.
        /// Calls <see cref="Refresh"/> to update rate label and bar.
        /// Public for EditMode test driving.
        /// Zero allocation.
        /// </summary>
        public void Tick(float dt)
        {
            if (_fastTimer > 0f)
            {
                _fastTimer -= dt;
                if (_fastTimer <= 0f)
                    _fastIndicator?.SetActive(false);
            }

            if (_slowTimer > 0f)
            {
                _slowTimer -= dt;
                if (_slowTimer <= 0f)
                    _slowIndicator?.SetActive(false);
            }

            Refresh();
        }

        /// <summary>
        /// Rebuilds the pace label and bar from the current SO state.
        /// Hides the panel when <c>_paceSO</c> is null.
        /// Zero allocation after Awake.
        /// </summary>
        public void Refresh()
        {
            if (_paceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            float rate = _paceSO.EventRate;

            if (_paceLabel != null)
                _paceLabel.text = $"{rate:F1}/s";

            if (_paceBar != null)
            {
                int threshold = Mathf.Max(1, _paceSO.FastThreshold);
                _paceBar.value = Mathf.Clamp01(rate / threshold);
            }
        }

        /// <summary>
        /// Shows the fast-pace indicator and starts its display timer.
        /// Hides the slow-pace indicator if active.
        /// Wired to <c>_onFastPace</c>.
        /// </summary>
        public void OnFastPace()
        {
            _fastTimer = _fastIndicatorDuration;
            _fastIndicator?.SetActive(true);
            _slowIndicator?.SetActive(false);
            _slowTimer = 0f;
        }

        /// <summary>
        /// Shows the slow-pace indicator and starts its display timer.
        /// Hides the fast-pace indicator if active.
        /// Wired to <c>_onSlowPace</c>.
        /// </summary>
        public void OnSlowPace()
        {
            _slowTimer = _slowIndicatorDuration;
            _slowIndicator?.SetActive(true);
            _fastIndicator?.SetActive(false);
            _fastTimer = 0f;
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>The assigned <see cref="MatchPaceSO"/>. May be null.</summary>
        public MatchPaceSO PaceSO => _paceSO;

        /// <summary>Remaining display time for the fast-pace indicator (seconds).</summary>
        public float FastTimer => _fastTimer;

        /// <summary>Remaining display time for the slow-pace indicator (seconds).</summary>
        public float SlowTimer => _slowTimer;

        /// <summary>Configured display duration for the fast-pace indicator.</summary>
        public float FastIndicatorDuration => _fastIndicatorDuration;

        /// <summary>Configured display duration for the slow-pace indicator.</summary>
        public float SlowIndicatorDuration => _slowIndicatorDuration;
    }
}

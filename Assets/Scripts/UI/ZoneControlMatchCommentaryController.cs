using System;
using BattleRobots.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that listens to up to four zone-control event channels and
    /// shows a timed commentary banner sourced from
    /// <see cref="ZoneControlCommentaryCatalogSO"/>.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   On each subscribed event, <see cref="ZoneControlCommentaryCatalogSO.GetMessage"/>
    ///   is called to retrieve the next round-robin message for that event type.
    ///   The banner is displayed for <c>_displayDuration</c> seconds and then
    ///   auto-hidden via <c>Update → Tick</c>.  Empty messages are silently ignored.
    ///
    /// ── Architecture notes ──────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace; no Physics references.
    ///   - All refs optional; null-safe throughout.
    ///   - Four delegates cached in Awake; zero alloc on subscribe/unsubscribe.
    ///   - DisallowMultipleComponent — one commentary banner per HUD.
    ///
    /// ── Scene wiring ──────────────────────────────────────────────────────────
    ///   1. Assign _catalogSO          → ZoneControlCommentaryCatalogSO asset.
    ///   2. Assign up to four event channels (all optional).
    ///   3. Assign _panel and _messageLabel.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlMatchCommentaryController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCommentaryCatalogSO _catalogSO;

        [Header("Display Settings")]
        [SerializeField, Min(0.1f)] private float _displayDuration = 2f;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onFastPace;
        [SerializeField] private VoidGameEvent _onSlowPace;
        [SerializeField] private VoidGameEvent _onRatingSet;
        [SerializeField] private VoidGameEvent _onZoneCaptured;

        [Header("UI Refs (optional)")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text       _messageLabel;

        // ── Runtime state ─────────────────────────────────────────────────────

        private float _displayTimer;
        private bool  _isActive;

        // ── Cached delegates ──────────────────────────────────────────────────

        private Action _handleFastPaceDelegate;
        private Action _handleSlowPaceDelegate;
        private Action _handleRatingSetDelegate;
        private Action _handleZoneCapturedDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _handleFastPaceDelegate     = HandleFastPace;
            _handleSlowPaceDelegate     = HandleSlowPace;
            _handleRatingSetDelegate    = HandleRatingSet;
            _handleZoneCapturedDelegate = HandleZoneCaptured;
        }

        private void OnEnable()
        {
            _onFastPace?.RegisterCallback(_handleFastPaceDelegate);
            _onSlowPace?.RegisterCallback(_handleSlowPaceDelegate);
            _onRatingSet?.RegisterCallback(_handleRatingSetDelegate);
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            HideBanner();
        }

        private void OnDisable()
        {
            _onFastPace?.UnregisterCallback(_handleFastPaceDelegate);
            _onSlowPace?.UnregisterCallback(_handleSlowPaceDelegate);
            _onRatingSet?.UnregisterCallback(_handleRatingSetDelegate);
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
        }

        private void Update() => Tick(Time.deltaTime);

        // ── Event handlers ────────────────────────────────────────────────────

        /// <summary>Shows a fast-pace commentary message.</summary>
        public void HandleFastPace()     => ShowBanner(ZoneControlCommentaryEventType.FastPace);

        /// <summary>Shows a slow-pace commentary message.</summary>
        public void HandleSlowPace()     => ShowBanner(ZoneControlCommentaryEventType.SlowPace);

        /// <summary>Shows a rating-set commentary message.</summary>
        public void HandleRatingSet()    => ShowBanner(ZoneControlCommentaryEventType.RatingSet);

        /// <summary>Shows a zone-captured commentary message.</summary>
        public void HandleZoneCaptured() => ShowBanner(ZoneControlCommentaryEventType.ZoneCaptured);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Retrieves the next message for <paramref name="eventType"/> from the
        /// catalog and shows it in the banner.  Silently ignores empty messages or
        /// a null catalog.
        /// </summary>
        public void ShowBanner(ZoneControlCommentaryEventType eventType)
        {
            if (_catalogSO == null) return;

            string msg = _catalogSO.GetMessage(eventType);
            if (string.IsNullOrEmpty(msg)) return;

            _isActive      = true;
            _displayTimer  = _displayDuration;
            _panel?.SetActive(true);

            if (_messageLabel != null)
                _messageLabel.text = msg;
        }

        /// <summary>
        /// Advances the auto-hide timer.
        /// Called from <c>Update</c> with <c>Time.deltaTime</c>.
        /// </summary>
        public void Tick(float dt)
        {
            if (!_isActive) return;

            _displayTimer -= dt;
            if (_displayTimer <= 0f)
                HideBanner();
        }

        /// <summary>Immediately hides the banner.</summary>
        public void HideBanner()
        {
            _isActive = false;
            _panel?.SetActive(false);
        }

        // ── Properties ────────────────────────────────────────────────────────

        /// <summary>Whether the banner is currently visible.</summary>
        public bool IsActive      => _isActive;

        /// <summary>Remaining display time in seconds.</summary>
        public float DisplayTimer => _displayTimer;

        /// <summary>The bound commentary catalog SO (may be null).</summary>
        public ZoneControlCommentaryCatalogSO CatalogSO => _catalogSO;
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace BattleRobots.UI
{
    /// <summary>
    /// HUD component that shows the current network latency.
    ///
    /// The label text and colour are updated only when the wallet fires its event
    /// (via an <c>IntGameEventListener</c> wired in the Inspector) — no Update loop,
    /// zero per-frame allocations.
    ///
    /// Colour thresholds:
    ///   0 – 80 ms  → green  (good)
    ///   81 – 150 ms → yellow (acceptable)
    ///   151+ ms    → red    (poor)
    ///   0 (offline) → grey
    ///
    /// Wiring in the Inspector:
    ///   □ _pingLabel          → Text component
    ///   □ _offlineLabel       → optional Text shown when ping is 0 (offline)
    ///   □ Add IntGameEventListener sibling; set its _event to PingSO's _onPingChanged;
    ///     set its Response to PingDisplayUI.OnPingChanged(int)
    ///
    /// BattleRobots.UI namespace — no Physics references.
    /// No Update / FixedUpdate declared.
    /// </summary>
    public sealed class PingDisplayUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Labels")]
        [Tooltip("Text label showing e.g. '42 ms'. Updated by OnPingChanged.")]
        [SerializeField] private Text _pingLabel;

        [Tooltip("Optional separate label shown when ping is 0 (offline / not connected).")]
        [SerializeField] private Text _offlineLabel;

        [Header("Colour Thresholds")]
        [Tooltip("Ping ≤ this value is considered good (green). Default 80 ms.")]
        [SerializeField, Min(1)] private int _goodThresholdMs = 80;

        [Tooltip("Ping ≤ this value is considered acceptable (yellow). Default 150 ms.")]
        [SerializeField, Min(1)] private int _acceptableThresholdMs = 150;

        [Header("Colours")]
        [SerializeField] private Color _goodColor       = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color _acceptableColor = new Color(1.0f, 0.85f, 0.1f);
        [SerializeField] private Color _poorColor       = new Color(0.9f, 0.2f, 0.2f);
        [SerializeField] private Color _offlineColor    = new Color(0.6f, 0.6f, 0.6f);

        // ── Public API (IntGameEventListener → OnPingChanged) ────────────────

        /// <summary>
        /// Called by an <c>IntGameEventListener</c> wired to <c>PingSO._onPingChanged</c>.
        /// Updates the label text and colour to reflect the new latency.
        /// </summary>
        public void OnPingChanged(int pingMs)
        {
            bool isOffline = pingMs <= 0;

            if (_offlineLabel != null)
                _offlineLabel.gameObject.SetActive(isOffline);

            if (_pingLabel == null) return;

            if (isOffline)
            {
                _pingLabel.gameObject.SetActive(false);
                return;
            }

            _pingLabel.gameObject.SetActive(true);
            _pingLabel.text  = $"{pingMs} ms";
            _pingLabel.color = PingColor(pingMs);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Color PingColor(int pingMs)
        {
            if (pingMs <= _goodThresholdMs)       return _goodColor;
            if (pingMs <= _acceptableThresholdMs)  return _acceptableColor;
            return _poorColor;
        }
    }
}

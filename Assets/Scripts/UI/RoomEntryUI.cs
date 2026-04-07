using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Prefab row component for a single room in the <see cref="RoomListUI"/> browser.
    ///
    /// Data is pushed in via <see cref="Setup(RoomEntry, Action{string})"/>; this
    /// component holds no direct reference to <see cref="RoomListSO"/> or any network
    /// adapter — keeping it decoupled from the data source.
    ///
    /// ARCHITECTURE RULES:
    ///   • BattleRobots.UI namespace — no Physics references.
    ///   • No per-frame cost — no Update / FixedUpdate defined.
    ///   • Allocation only in Awake (AddListener) and Setup (closure captured action).
    ///
    /// Inspector wiring:
    ///   □ _roomCodeLabel        → Text displaying the 4-char room code
    ///   □ _playerCountLabel     → Text displaying "N/MAX" player capacity
    ///   □ _fullBadge            → (optional) GameObject shown only when the room is full
    ///   □ _privateBadge         → (optional) GameObject shown when the room is private
    ///   □ _joinButton           → Button that triggers the join action (disabled when full)
    ///   □ _favouriteButton      → (optional) FavouriteButtonUI child component
    ///   □ _copyButton           → (optional) Button that copies room code to clipboard
    ///   □ _copiedFeedbackLabel  → (optional) Text showing "Copied!" for 1.5 s after copy
    ///   □ _pingBadge            → (optional) Image coloured by latency (grey/green/yellow/red)
    ///   □ _pingLabel            → (optional) Text showing "N ms" (empty when pingMs = 0)
    ///   □ _hostNameLabel        → (optional) Text showing the host's display name
    ///   □ _ageLabel             → (optional) Text showing relative room age ("Just now", "3m ago", etc.)
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoomEntryUI : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Labels")]
        [Tooltip("Text component that shows the room code.")]
        [SerializeField] private Text _roomCodeLabel;

        [Tooltip("Text component that shows the player count as 'N/MAX'.")]
        [SerializeField] private Text _playerCountLabel;

        [Tooltip("(Optional) Text component showing how many slots are still open, e.g. '1 left'. " +
                 "Hidden (empty string) when the room is full or maxPlayers is not configured.")]
        [SerializeField] private Text _slotsRemainingLabel;

        [Tooltip("(Optional) GameObject shown when the room has reached capacity. " +
                 "Use a Text child labelled 'FULL' or a coloured overlay.")]
        [SerializeField] private GameObject _fullBadge;

        [Tooltip("(Optional) GameObject shown when the room is private (requires a password). " +
                 "Use a lock icon or Text labelled 'PRIVATE'.")]
        [SerializeField] private GameObject _privateBadge;

        [Header("Action")]
        [Tooltip("Button the user presses to join this room. Disabled when the room is full.")]
        [SerializeField] private Button _joinButton;

        [Header("Favourite (optional)")]
        [Tooltip("(Optional) FavouriteButtonUI child component. " +
                 "Setup() will wire it automatically when a FavouriteRoomsSO is provided.")]
        [SerializeField] private FavouriteButtonUI _favouriteButton;

        [Header("Copy to Clipboard (optional)")]
        [Tooltip("(Optional) Button that copies the room code to the system clipboard.")]
        [SerializeField] private Button _copyButton;

        [Tooltip("(Optional) Text label that briefly shows 'Copied!' for 1.5 s after the " +
                 "copy button is pressed, then reverts to empty.")]
        [SerializeField] private Text _copiedFeedbackLabel;

        [Header("Ping Badge (optional)")]
        [Tooltip("(Optional) Image used as a coloured latency indicator dot. " +
                 "Colour: grey = unknown (0 ms), green ≤ 80 ms, yellow ≤ 150 ms, red ≥ 151 ms.")]
        [SerializeField] private Image _pingBadge;

        [Tooltip("(Optional) Text label showing the numeric latency, e.g. '42 ms'. " +
                 "Empty when pingMs is 0 (unknown).")]
        [SerializeField] private Text _pingLabel;

        [Header("Host Name (optional)")]
        [Tooltip("(Optional) Text label showing the display name of the room host. " +
                 "Hidden (empty string) when hostName is not provided.")]
        [SerializeField] private Text _hostNameLabel;

        [Header("Room Age (optional)")]
        [Tooltip("(Optional) Text label showing how long ago the room was created, " +
                 "e.g. 'Just now', '3m ago', '2h ago'. Hidden (empty string) when " +
                 "createdAt is 0 (unknown).")]
        [SerializeField] private Text _ageLabel;

        // ── Runtime state ─────────────────────────────────────────────────────

        private Action<string> _onJoin;
        private string         _roomCode = string.Empty;

        /// <summary>
        /// The room code most recently copied to the system clipboard via this button.
        /// Empty string until <see cref="HandleCopyClicked"/> has been invoked at least once.
        /// Primarily useful for testing.
        /// </summary>
        public string LastCopiedCode { get; private set; } = string.Empty;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_joinButton != null)
                _joinButton.onClick.AddListener(HandleJoinClicked);

            if (_copyButton != null)
                _copyButton.onClick.AddListener(HandleCopyClicked);
        }

        private void OnDestroy()
        {
            if (_joinButton != null)
                _joinButton.onClick.RemoveListener(HandleJoinClicked);

            if (_copyButton != null)
                _copyButton.onClick.RemoveListener(HandleCopyClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Configure this row for the given <paramref name="entry"/>.
        /// <paramref name="onJoin"/> is invoked with the room code when the
        /// Join button is pressed. Call once immediately after instantiation.
        /// </summary>
        public void Setup(RoomEntry entry, Action<string> onJoin)
            => Setup(entry, onJoin, null);

        /// <summary>
        /// Configure this row for the given <paramref name="entry"/> with optional
        /// favourite-room support.
        /// <paramref name="onJoin"/> is invoked with the room code when the Join button
        /// is pressed. <paramref name="favourites"/> wires the star button — pass
        /// <c>null</c> to hide/disable favourite functionality.
        /// </summary>
        public void Setup(RoomEntry entry, Action<string> onJoin, BattleRobots.Core.FavouriteRoomsSO favourites)
        {
            _roomCode = entry.roomCode ?? string.Empty;
            _onJoin   = onJoin;

            if (_roomCodeLabel != null)
                _roomCodeLabel.text = _roomCode;

            if (_playerCountLabel != null)
            {
                // Format: "1/2" when capacity is known; fall back to plain count.
                _playerCountLabel.text = entry.maxPlayers > 0
                    ? $"{entry.playerCount}/{entry.maxPlayers}"
                    : entry.playerCount.ToString();
            }

            bool isFull    = entry.IsFull;
            bool isPrivate = entry.isPrivate;

            // Slots-remaining label: show "N left" only when room is not full and
            // maxPlayers is configured. Empty string hides the label gracefully.
            if (_slotsRemainingLabel != null)
            {
                _slotsRemainingLabel.text = entry.SlotsRemaining > 0
                    ? $"{entry.SlotsRemaining} left"
                    : string.Empty;
            }

            if (_fullBadge != null)
                _fullBadge.SetActive(isFull);

            if (_privateBadge != null)
                _privateBadge.SetActive(isPrivate);

            if (_joinButton != null)
                _joinButton.interactable = !string.IsNullOrEmpty(_roomCode) && !isFull;

            if (_copyButton != null)
                _copyButton.interactable = !string.IsNullOrEmpty(_roomCode);

            // Wire the favourite star button if one is present and a SO was provided.
            if (_favouriteButton != null)
            {
                _favouriteButton.gameObject.SetActive(favourites != null);
                if (favourites != null)
                    _favouriteButton.Setup(favourites, _roomCode);
            }

            // Host name label: show the room owner's display name, or hide when absent.
            if (_hostNameLabel != null)
                _hostNameLabel.text = entry.hostName ?? string.Empty;

            // Apply the ping latency badge (colour dot + numeric label).
            ApplyPingBadge(entry.pingMs);

            // Room age label: compute relative time string from createdAt ticks.
            if (_ageLabel != null)
                _ageLabel.text = GetAgeString(entry.createdAt, DateTime.UtcNow.Ticks);
        }

        /// <summary>
        /// Updates the ping badge colour and label for the given latency.
        /// Colour thresholds match <see cref="PingDisplayUI"/>:
        ///   grey = 0 (unknown), green ≤ 80 ms, yellow ≤ 150 ms, red ≥ 151 ms.
        /// </summary>
        private void ApplyPingBadge(int pingMs)
        {
            if (_pingBadge != null)
                _pingBadge.color = GetPingColor(pingMs);

            if (_pingLabel != null)
                _pingLabel.text = pingMs > 0 ? $"{pingMs} ms" : string.Empty;
        }

        /// <summary>
        /// Maps a latency value to a display colour using the standard thresholds.
        /// Exposed as public static so tests can verify the mapping without instantiating
        /// the full MonoBehaviour.
        ///   0 ms  → grey  (unknown / not measured)
        ///   ≤ 80  → green (excellent)
        ///   ≤ 150 → yellow (acceptable)
        ///   ≥ 151 → red   (high latency)
        /// </summary>
        public static Color GetPingColor(int pingMs)
        {
            if (pingMs <= 0)   return new Color(0.5f, 0.5f, 0.5f); // grey — unknown
            if (pingMs <= 80)  return Color.green;
            if (pingMs <= 150) return Color.yellow;
            return Color.red;
        }

        /// <summary>
        /// Classifies a latency value into a <see cref="PingTier"/>.
        /// Exposed as public static so tests and <see cref="RoomListUI"/> can use it
        /// without instantiating this MonoBehaviour.
        ///   0 or negative  → <see cref="PingTier.Unknown"/>
        ///   ≤ 80 ms        → <see cref="PingTier.Excellent"/>
        ///   ≤ 150 ms       → <see cref="PingTier.Good"/>
        ///   &gt; 150 ms    → <see cref="PingTier.High"/>
        /// </summary>
        public static PingTier GetPingTier(int pingMs)
        {
            if (pingMs <= 0)   return PingTier.Unknown;
            if (pingMs <= 80)  return PingTier.Excellent;
            if (pingMs <= 150) return PingTier.Good;
            return PingTier.High;
        }

        /// <summary>
        /// Returns the human-readable section header label for a given
        /// <see cref="PingTier"/>. Used by <see cref="RoomListUI"/> when
        /// <c>_groupByPingTier</c> is enabled.
        /// </summary>
        public static string GetTierLabel(PingTier tier)
        {
            switch (tier)
            {
                case PingTier.Excellent: return "Excellent  \u2264 80 ms";
                case PingTier.Good:      return "Good  \u2264 150 ms";
                case PingTier.High:      return "High  > 150 ms";
                default:                 return "Unknown";
            }
        }

        /// <summary>
        /// Converts a room creation timestamp into a human-readable relative age string.
        ///
        /// Exposed as public static so unit tests can verify the mapping without
        /// instantiating the full MonoBehaviour.
        ///
        /// Rules:
        ///   0 or negative createdAt → empty string (unknown creation time)
        ///   age &lt;  60 seconds       → "Just now"
        ///   age &lt;  60 minutes       → "Xm ago"
        ///   age &lt;  24 hours         → "Xh ago"
        ///   age ≥  24 hours         → "Xd ago"
        /// </summary>
        /// <param name="createdAtTicks">UTC ticks of room creation (from <see cref="RoomEntry.createdAt"/>).</param>
        /// <param name="nowTicks">UTC ticks representing "now". In production pass <c>DateTime.UtcNow.Ticks</c>.</param>
        public static string GetAgeString(long createdAtTicks, long nowTicks)
        {
            if (createdAtTicks <= 0L) return string.Empty;

            long elapsedTicks = nowTicks - createdAtTicks;
            if (elapsedTicks < 0L) return string.Empty; // clock skew / future timestamp

            double seconds = (double)elapsedTicks / TimeSpan.TicksPerSecond;

            if (seconds < 60.0)          return "Just now";
            if (seconds < 3600.0)        return $"{(int)(seconds / 60.0)}m ago";
            if (seconds < 86400.0)       return $"{(int)(seconds / 3600.0)}h ago";
            return                              $"{(int)(seconds / 86400.0)}d ago";
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void HandleJoinClicked()
        {
            _onJoin?.Invoke(_roomCode);
        }

        /// <summary>
        /// Copies the current room code to the system clipboard and briefly shows
        /// "Copied!" in the feedback label (if wired). Safe to call even when no
        /// room code is set — no-ops in that case.
        /// </summary>
        public void HandleCopyClicked()
        {
            if (string.IsNullOrEmpty(_roomCode)) return;

            GUIUtility.systemCopyBuffer = _roomCode;
            LastCopiedCode = _roomCode;

            if (_copiedFeedbackLabel != null)
            {
                _copiedFeedbackLabel.text = "Copied!";
                // Stop any in-progress clear so only one runs at a time.
                StopCoroutine(nameof(ClearCopiedFeedback));
                StartCoroutine(nameof(ClearCopiedFeedback));
            }
        }

        private IEnumerator ClearCopiedFeedback()
        {
            yield return new WaitForSeconds(1.5f);
            if (_copiedFeedbackLabel != null)
                _copiedFeedbackLabel.text = string.Empty;
        }
    }
}

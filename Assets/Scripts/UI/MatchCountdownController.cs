using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// Displays the pre-match countdown ("3", "2", "1", "FIGHT!") by reacting to
    /// <see cref="CountdownManager"/> SO event channels.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   <c>CountdownManager</c> fires <c>_onCountdownTick(int)</c> with values 3, 2, 1
    ///   → <see cref="HandleTick"/>: shows panel, updates number text.
    ///   <c>CountdownManager</c> fires <c>_onCountdownComplete</c> (zero moment)
    ///   → <see cref="HandleComplete"/>: sets text to "FIGHT!", starts hide coroutine.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Add this MB to any persistent Canvas GameObject.
    ///   2. Assign <c>_onCountdownTick</c>     → the IntGameEvent SO from CountdownManager.
    ///   3. Assign <c>_onCountdownComplete</c>  → the VoidGameEvent SO from CountdownManager.
    ///   4. Assign <c>_countdownPanel</c> (optional) — shown on first tick, hidden after FIGHT!.
    ///   5. Assign <c>_countdownText</c> (optional UnityEngine.UI.Text) for number display.
    ///   6. Tune <c>_fightDisplaySeconds</c> (default 1 s) — how long "FIGHT!" stays visible.
    ///
    /// ── Architecture notes ────────────────────────────────────────────────────
    ///   - BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   - All Action delegates are cached in Awake — zero alloc in event callbacks.
    ///   - No Update / FixedUpdate — purely event-driven.
    ///   - <see cref="WaitForSecondsRealtime"/> keeps the "FIGHT!" label visible even if
    ///     Time.timeScale is 0 (defensive edge-case safety during match start).
    /// </summary>
    public sealed class MatchCountdownController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Event Channels — In")]
        [Tooltip("IntGameEvent raised by CountdownManager once per tick (e.g., 3, 2, 1). " +
                 "Assign the same SO as CountdownManager._onCountdownTick.")]
        [SerializeField] private IntGameEvent _onCountdownTick;

        [Tooltip("VoidGameEvent raised by CountdownManager when countdown reaches zero. " +
                 "Assign the same SO as CountdownManager._onCountdownComplete.")]
        [SerializeField] private VoidGameEvent _onCountdownComplete;

        [Header("Display (all optional)")]
        [Tooltip("Root panel shown on first tick and hidden after the FIGHT! display period.")]
        [SerializeField] private GameObject _countdownPanel;

        [Tooltip("Text label for countdown numbers (e.g., '3') and the 'FIGHT!' message.")]
        [SerializeField] private Text _countdownText;

        [Tooltip("Seconds the 'FIGHT!' message remains visible before the panel is hidden. " +
                 "Uses WaitForSecondsRealtime so paused timeScale does not affect the delay.")]
        [SerializeField, Min(0f)] private float _fightDisplaySeconds = 1f;

        // ── Cached delegates (allocated once in Awake — zero alloc thereafter) ─

        private Action<int> _tickHandler;
        private Action      _completeHandler;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _tickHandler     = HandleTick;
            _completeHandler = HandleComplete;

            // Start panel hidden; it will be shown on the first incoming tick.
            if (_countdownPanel != null)
                _countdownPanel.SetActive(false);
        }

        private void OnEnable()
        {
            _onCountdownTick?.RegisterCallback(_tickHandler);
            _onCountdownComplete?.RegisterCallback(_completeHandler);
        }

        private void OnDisable()
        {
            _onCountdownTick?.UnregisterCallback(_tickHandler);
            _onCountdownComplete?.UnregisterCallback(_completeHandler);
            StopAllCoroutines();
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void HandleTick(int count)
        {
            if (_countdownPanel != null)
                _countdownPanel.SetActive(true);

            if (_countdownText != null)
                _countdownText.text = count.ToString();
        }

        private void HandleComplete()
        {
            if (_countdownText != null)
                _countdownText.text = "FIGHT!";

            if (_fightDisplaySeconds > 0f)
                StartCoroutine(HideAfterDelay());
            else
                HidePanel();
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(_fightDisplaySeconds);
            HidePanel();
        }

        private void HidePanel()
        {
            if (_countdownPanel != null)
                _countdownPanel.SetActive(false);
        }

        // ── Editor validation ─────────────────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_onCountdownTick == null)
                Debug.LogWarning("[MatchCountdownController] _onCountdownTick IntGameEvent " +
                                 "not assigned — countdown display will not update.", this);
            if (_onCountdownComplete == null)
                Debug.LogWarning("[MatchCountdownController] _onCountdownComplete VoidGameEvent " +
                                 "not assigned — 'FIGHT!' will never be shown.", this);
        }
#endif
    }
}

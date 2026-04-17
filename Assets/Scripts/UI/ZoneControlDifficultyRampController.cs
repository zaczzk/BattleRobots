using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that drives the difficulty-ramp lifecycle and displays
    /// the current normalised difficulty value.
    ///
    /// ── Behaviour ──────────────────────────────────────────────────────────────
    ///   <c>_onMatchStarted</c>: calls <see cref="ZoneControlDifficultyRampSO.StartRamp"/>.
    ///   <c>_onMatchEnded</c>: calls <see cref="ZoneControlDifficultyRampSO.Reset"/>.
    ///   <c>_onDifficultyChanged</c>: refreshes the difficulty label.
    ///   <see cref="Update"/> ticks the ramp each frame while active.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlDifficultyRampController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlDifficultyRampSO _rampSO;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMatchEnded;
        [SerializeField] private VoidGameEvent _onDifficultyChanged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _difficultyLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleMatchStartedDelegate;
        private Action _handleMatchEndedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMatchEndedDelegate   = HandleMatchEnded;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.RegisterCallback(_handleMatchEndedDelegate);
            _onDifficultyChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMatchEnded?.UnregisterCallback(_handleMatchEndedDelegate);
            _onDifficultyChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            _rampSO?.Tick(Time.deltaTime);
        }

        private void HandleMatchStarted() => _rampSO?.StartRamp();
        private void HandleMatchEnded()   => _rampSO?.Reset();

        public void Refresh()
        {
            if (_rampSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_difficultyLabel != null)
                _difficultyLabel.text = $"Difficulty: {_rampSO.CurrentDifficulty:F2}";
        }

        public ZoneControlDifficultyRampSO RampSO => _rampSO;
    }
}

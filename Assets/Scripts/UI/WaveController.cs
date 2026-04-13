using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// MonoBehaviour that drives a survival-wave HUD panel from
    /// <see cref="WaveManagerSO"/> state, updating on every wave event.
    ///
    /// ── Data flow ─────────────────────────────────────────────────────────────
    ///   OnEnable → subscribes _onWaveStarted + _onWaveCompleted → Refresh();
    ///              subscribes _onSurvivalEnded → Hide().
    ///   Refresh()  → reads WaveManagerSO; hides panel when null or not IsActive;
    ///                otherwise updates _waveLabel, _botsLabel, _bestWaveLabel
    ///                and shows _survivalPanel.
    ///   OnDisable  → unsubscribes all channels, does NOT hide the panel
    ///                (scene teardown leaves the panel state intact).
    ///
    /// ── ARCHITECTURE RULES ────────────────────────────────────────────────────
    ///   • BattleRobots.UI namespace — must NOT reference BattleRobots.Physics.
    ///   • Delegates cached in Awake; zero alloc after initialisation.
    ///   • No Update / FixedUpdate — purely event-driven.
    ///   • All fields optional and null-guarded.
    ///
    /// ── Scene wiring instructions ─────────────────────────────────────────────
    ///   1. Assign _waveManager → the WaveManagerSO asset.
    ///   2. Assign _onWaveStarted, _onWaveCompleted, _onSurvivalEnded →
    ///      the matching VoidGameEvent assets from WaveManagerSO.
    ///   3. Assign _survivalPanel → the root GameObject of the wave HUD panel.
    ///   4. Assign _waveLabel, _botsLabel, _bestWaveLabel → Text components
    ///      inside the panel (all optional).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WaveController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────

        [Header("Data")]
        [Tooltip("Runtime SO that holds all wave state. Assign the same asset as " +
                 "the one used by your survival game logic.")]
        [SerializeField] private WaveManagerSO _waveManager;

        [Header("Event Channels In")]
        [Tooltip("VoidGameEvent raised when a new wave starts. → Refresh().")]
        [SerializeField] private VoidGameEvent _onWaveStarted;

        [Tooltip("VoidGameEvent raised when the current wave is completed. → Refresh().")]
        [SerializeField] private VoidGameEvent _onWaveCompleted;

        [Tooltip("VoidGameEvent raised when the survival run ends. → Hide().")]
        [SerializeField] private VoidGameEvent _onSurvivalEnded;

        [Header("UI Refs (all optional)")]
        [Tooltip("Root panel GameObject shown while a survival run is active.")]
        [SerializeField] private GameObject _survivalPanel;

        [Tooltip("Text displaying the current wave number (e.g. 'Wave 3').")]
        [SerializeField] private Text _waveLabel;

        [Tooltip("Text displaying how many bots remain in the current wave.")]
        [SerializeField] private Text _botsLabel;

        [Tooltip("Text showing the all-time best wave reached (e.g. 'Best: Wave 7').")]
        [SerializeField] private Text _bestWaveLabel;

        // ── Cached delegates (allocated once in Awake — zero alloc thereafter) ─

        private System.Action _refreshDelegate;
        private System.Action _hideDelegate;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _refreshDelegate = Refresh;
            _hideDelegate    = Hide;
        }

        private void OnEnable()
        {
            _onWaveStarted?.RegisterCallback(_refreshDelegate);
            _onWaveCompleted?.RegisterCallback(_refreshDelegate);
            _onSurvivalEnded?.RegisterCallback(_hideDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onWaveStarted?.UnregisterCallback(_refreshDelegate);
            _onWaveCompleted?.UnregisterCallback(_refreshDelegate);
            _onSurvivalEnded?.UnregisterCallback(_hideDelegate);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Syncs all HUD labels with the current <see cref="WaveManagerSO"/> state.
        /// Hides <see cref="_survivalPanel"/> when the manager is null or the run
        /// is not active.
        /// </summary>
        public void Refresh()
        {
            if (_waveManager == null || !_waveManager.IsActive)
            {
                Hide();
                return;
            }

            _survivalPanel?.SetActive(true);
            if (_waveLabel    != null) _waveLabel.text    = "Wave " + _waveManager.CurrentWave;
            if (_botsLabel    != null) _botsLabel.text    = _waveManager.BotsRemainingInWave + " bots remaining";
            if (_bestWaveLabel != null) _bestWaveLabel.text = "Best: Wave " + _waveManager.BestWave;
        }

        /// <summary>Hides <see cref="_survivalPanel"/>. No-op when the panel ref is null.</summary>
        public void Hide()
        {
            _survivalPanel?.SetActive(false);
        }
    }
}

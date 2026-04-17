using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that wires zone-capture events into
    /// <see cref="ZoneControlCaptureComboMultiplierSO"/>, awards a wallet bonus
    /// scaled by the current multiplier on each capture, and shows a
    /// "Combo: xF1" label with a combo-window progress bar.
    ///
    /// <c>_onZoneCaptured</c>: RecordCapture(Time.time) + wallet bonus + Refresh.
    /// <c>_onMatchStarted</c>: Reset + Refresh.
    /// <c>_onComboMultiplierChanged</c>: Refresh.
    /// Update: Tick(Time.deltaTime).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureComboMultiplierController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureComboMultiplierSO _comboSO;
        [SerializeField] private PlayerWallet                         _wallet;

        [Header("Bonus Per Capture (base amount before multiplier)")]
        [Min(0)]
        [SerializeField] private int _baseBonusPerCapture = 50;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onComboMultiplierChanged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _multiplierLabel;
        [SerializeField] private Slider     _comboBar;
        [SerializeField] private GameObject _panel;

        private Action _handleZoneCapturedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _refreshDelegate;

        private void Awake()
        {
            _handleZoneCapturedDelegate = HandleZoneCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _refreshDelegate            = Refresh;
        }

        private void OnEnable()
        {
            _onZoneCaptured?.RegisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onComboMultiplierChanged?.RegisterCallback(_refreshDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onZoneCaptured?.UnregisterCallback(_handleZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onComboMultiplierChanged?.UnregisterCallback(_refreshDelegate);
        }

        private void Update()
        {
            if (_comboSO != null)
                _comboSO.Tick(Time.deltaTime);
        }

        private void HandleZoneCaptured()
        {
            if (_comboSO == null)
            {
                Refresh();
                return;
            }

            _comboSO.RecordCapture(Time.time);

            if (_wallet != null && _baseBonusPerCapture > 0)
                _wallet.AddFunds(_comboSO.ComputeBonus(_baseBonusPerCapture));

            Refresh();
        }

        private void HandleMatchStarted()
        {
            _comboSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_comboSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_multiplierLabel != null)
                _multiplierLabel.text = $"Combo: x{_comboSO.CurrentMultiplier:F1}";

            if (_comboBar != null)
                _comboBar.value = Mathf.Clamp01(_comboSO.CurrentMultiplier / _comboSO.MaxMultiplier);
        }

        public ZoneControlCaptureComboMultiplierSO ComboSO => _comboSO;
    }
}

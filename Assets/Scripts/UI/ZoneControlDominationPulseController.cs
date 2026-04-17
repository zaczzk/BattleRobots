using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that bridges <see cref="ZoneDominanceSO"/> state changes
    /// into <see cref="ZoneControlDominationPulseSO"/>, awards wallet bonuses on
    /// each pulse, and displays pulse progress and totals.
    ///
    /// <c>_onDominanceChanged</c>: reads <c>_dominanceSO.HasDominance</c> and
    ///   calls <c>StartDomination</c> or <c>EndDomination</c>.
    /// <c>_onMatchStarted</c>: resets the pulse SO + Refresh.
    /// <c>_onPulseTriggered</c>: awards <c>_wallet.AddFunds(PulseBonus)</c> + Refresh.
    /// <see cref="Update"/> ticks the pulse SO each frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlDominationPulseController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlDominationPulseSO _pulseSO;
        [SerializeField] private ZoneDominanceSO              _dominanceSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onDominanceChanged;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPulseTriggered;

        [Header("UI References (optional)")]
        [SerializeField] private Slider     _progressBar;
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handleDominanceChangedDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePulseTriggeredDelegate;

        private void Awake()
        {
            _handleDominanceChangedDelegate = HandleDominanceChanged;
            _handleMatchStartedDelegate     = HandleMatchStarted;
            _handlePulseTriggeredDelegate   = HandlePulseTriggered;
        }

        private void OnEnable()
        {
            _onDominanceChanged?.RegisterCallback(_handleDominanceChangedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPulseTriggered?.RegisterCallback(_handlePulseTriggeredDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onDominanceChanged?.UnregisterCallback(_handleDominanceChangedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPulseTriggered?.UnregisterCallback(_handlePulseTriggeredDelegate);
        }

        private void Update()
        {
            _pulseSO?.Tick(Time.deltaTime);
        }

        private void HandleDominanceChanged()
        {
            if (_pulseSO == null) return;
            if (_dominanceSO != null && _dominanceSO.HasDominance)
                _pulseSO.StartDomination();
            else
                _pulseSO.EndDomination();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _pulseSO?.Reset();
            Refresh();
        }

        private void HandlePulseTriggered()
        {
            if (_pulseSO != null && _wallet != null)
                _wallet.AddFunds(_pulseSO.PulseBonus);
            Refresh();
        }

        public void Refresh()
        {
            if (_pulseSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_progressBar != null)
                _progressBar.value = _pulseSO.PulseProgress;

            if (_statusLabel != null)
                _statusLabel.text = _pulseSO.IsDominating ? "Dominating!" : "Waiting";

            if (_bonusLabel != null)
                _bonusLabel.text =
                    $"Pulses: {_pulseSO.TotalPulseCount} | Bonus: {_pulseSO.TotalBonusAwarded}";
        }

        public ZoneControlDominationPulseSO PulseSO => _pulseSO;
    }
}

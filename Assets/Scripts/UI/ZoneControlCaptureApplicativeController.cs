using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureApplicativeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureApplicativeSO _applicativeSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onApplicativeApplied;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _appliedLabel;
        [SerializeField] private Text       _applyLabel;
        [SerializeField] private Slider     _appliedBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleApplicativeAppliedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate             = HandlePlayerCaptured;
            _handleBotDelegate                = HandleBotCaptured;
            _handleMatchStartedDelegate       = HandleMatchStarted;
            _handleApplicativeAppliedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onApplicativeApplied?.RegisterCallback(_handleApplicativeAppliedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onApplicativeApplied?.UnregisterCallback(_handleApplicativeAppliedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_applicativeSO == null) return;
            int bonus = _applicativeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_applicativeSO == null) return;
            _applicativeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _applicativeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_applicativeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_appliedLabel != null)
                _appliedLabel.text = $"Applied: {_applicativeSO.Applications}/{_applicativeSO.ApplicationsNeeded}";

            if (_applyLabel != null)
                _applyLabel.text = $"Applies: {_applicativeSO.ApplyCount}";

            if (_appliedBar != null)
                _appliedBar.value = _applicativeSO.ApplicationProgress;
        }

        public ZoneControlCaptureApplicativeSO ApplicativeSO => _applicativeSO;
    }
}

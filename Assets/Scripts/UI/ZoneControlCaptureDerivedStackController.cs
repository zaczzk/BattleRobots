using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDerivedStackController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDerivedStackSO _derivedStackSO;
        [SerializeField] private PlayerWallet                      _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDerivedStackResolved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _derivedThickeningLabel;
        [SerializeField] private Text       _resolutionLabel;
        [SerializeField] private Slider     _derivedThickeningBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleResolvedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleResolvedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDerivedStackResolved?.RegisterCallback(_handleResolvedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDerivedStackResolved?.UnregisterCallback(_handleResolvedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_derivedStackSO == null) return;
            int bonus = _derivedStackSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_derivedStackSO == null) return;
            _derivedStackSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _derivedStackSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_derivedStackSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_derivedThickeningLabel != null)
                _derivedThickeningLabel.text = $"Derived Thickenings: {_derivedStackSO.DerivedThickenings}/{_derivedStackSO.DerivedThickeningsNeeded}";

            if (_resolutionLabel != null)
                _resolutionLabel.text = $"Resolutions: {_derivedStackSO.ResolutionCount}";

            if (_derivedThickeningBar != null)
                _derivedThickeningBar.value = _derivedStackSO.DerivedThickeningProgress;
        }

        public ZoneControlCaptureDerivedStackSO DerivedStackSO => _derivedStackSO;
    }
}

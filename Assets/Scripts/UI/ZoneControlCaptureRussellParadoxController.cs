using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRussellParadoxController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRussellParadoxSO _russellParadoxSO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRussellParadoxResolved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _selfContainingSetLabel;
        [SerializeField] private Text       _resolutionCountLabel;
        [SerializeField] private Slider     _selfContainingSetBar;
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
            _onRussellParadoxResolved?.RegisterCallback(_handleResolvedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRussellParadoxResolved?.UnregisterCallback(_handleResolvedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_russellParadoxSO == null) return;
            int bonus = _russellParadoxSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_russellParadoxSO == null) return;
            _russellParadoxSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _russellParadoxSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_russellParadoxSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_selfContainingSetLabel != null)
                _selfContainingSetLabel.text =
                    $"Self-Containing Sets: {_russellParadoxSO.SelfContainingSets}/{_russellParadoxSO.SelfContainingSetsNeeded}";

            if (_resolutionCountLabel != null)
                _resolutionCountLabel.text = $"Resolutions: {_russellParadoxSO.ResolutionCount}";

            if (_selfContainingSetBar != null)
                _selfContainingSetBar.value = _russellParadoxSO.SelfContainingSetProgress;
        }

        public ZoneControlCaptureRussellParadoxSO RussellParadoxSO => _russellParadoxSO;
    }
}

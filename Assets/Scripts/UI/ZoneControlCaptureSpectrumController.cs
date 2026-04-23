using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSpectrumController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSpectrumSO _spectrumSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSpectrumResolved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _bandLabel;
        [SerializeField] private Text       _resolveLabel;
        [SerializeField] private Slider     _bandBar;
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
            _onSpectrumResolved?.RegisterCallback(_handleResolvedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSpectrumResolved?.UnregisterCallback(_handleResolvedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_spectrumSO == null) return;
            int bonus = _spectrumSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_spectrumSO == null) return;
            _spectrumSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _spectrumSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_spectrumSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_bandLabel != null)
                _bandLabel.text = $"Bands: {_spectrumSO.Bands}/{_spectrumSO.BandsNeeded}";

            if (_resolveLabel != null)
                _resolveLabel.text = $"Resolutions: {_spectrumSO.ResolutionCount}";

            if (_bandBar != null)
                _bandBar.value = _spectrumSO.BandProgress;
        }

        public ZoneControlCaptureSpectrumSO SpectrumSO => _spectrumSO;
    }
}

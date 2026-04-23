using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSpectralController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSpectralSO _spectralSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSpectralConverged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _pageLabel;
        [SerializeField] private Text       _convergenceLabel;
        [SerializeField] private Slider     _pageBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleConvergedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleConvergedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSpectralConverged?.RegisterCallback(_handleConvergedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSpectralConverged?.UnregisterCallback(_handleConvergedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_spectralSO == null) return;
            int bonus = _spectralSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_spectralSO == null) return;
            _spectralSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _spectralSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_spectralSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_pageLabel != null)
                _pageLabel.text = $"Pages: {_spectralSO.Pages}/{_spectralSO.PagesNeeded}";

            if (_convergenceLabel != null)
                _convergenceLabel.text = $"Convergences: {_spectralSO.ConvergenceCount}";

            if (_pageBar != null)
                _pageBar.value = _spectralSO.PageProgress;
        }

        public ZoneControlCaptureSpectralSO SpectralSO => _spectralSO;
    }
}

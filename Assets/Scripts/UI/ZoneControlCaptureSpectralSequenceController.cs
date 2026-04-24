using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSpectralSequenceController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSpectralSequenceSO _spectralSequenceSO;
        [SerializeField] private PlayerWallet                          _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSpectralSequenceConverged;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _pageLabel;
        [SerializeField] private Text       _convergeLabel;
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
            _onSpectralSequenceConverged?.RegisterCallback(_handleConvergedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSpectralSequenceConverged?.UnregisterCallback(_handleConvergedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_spectralSequenceSO == null) return;
            int bonus = _spectralSequenceSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_spectralSequenceSO == null) return;
            _spectralSequenceSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _spectralSequenceSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_spectralSequenceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_pageLabel != null)
                _pageLabel.text = $"Pages: {_spectralSequenceSO.Pages}/{_spectralSequenceSO.PagesNeeded}";

            if (_convergeLabel != null)
                _convergeLabel.text = $"Convergences: {_spectralSequenceSO.ConvergeCount}";

            if (_pageBar != null)
                _pageBar.value = _spectralSequenceSO.PageProgress;
        }

        public ZoneControlCaptureSpectralSequenceSO SpectralSequenceSO => _spectralSequenceSO;
    }
}

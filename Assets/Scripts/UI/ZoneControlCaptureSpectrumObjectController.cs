using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSpectrumObjectController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSpectrumObjectSO _spectrumObjectSO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSpectrumObjectDelooped;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _loopLabel;
        [SerializeField] private Text       _deloopLabel;
        [SerializeField] private Slider     _loopBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDeloopedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDeloopedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSpectrumObjectDelooped?.RegisterCallback(_handleDeloopedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSpectrumObjectDelooped?.UnregisterCallback(_handleDeloopedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_spectrumObjectSO == null) return;
            int bonus = _spectrumObjectSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_spectrumObjectSO == null) return;
            _spectrumObjectSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _spectrumObjectSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_spectrumObjectSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_loopLabel != null)
                _loopLabel.text = $"Loops: {_spectrumObjectSO.Loops}/{_spectrumObjectSO.LoopsNeeded}";

            if (_deloopLabel != null)
                _deloopLabel.text = $"Deloopings: {_spectrumObjectSO.DeloopCount}";

            if (_loopBar != null)
                _loopBar.value = _spectrumObjectSO.LoopProgress;
        }

        public ZoneControlCaptureSpectrumObjectSO SpectrumObjectSO => _spectrumObjectSO;
    }
}

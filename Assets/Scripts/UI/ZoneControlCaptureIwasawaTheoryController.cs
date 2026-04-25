using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureIwasawaTheoryController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureIwasawaTheorySO _iwasawaTheorySO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onIwasawaTheoryInterpolated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _padicFunctionLabel;
        [SerializeField] private Text       _interpolationLabel;
        [SerializeField] private Slider     _padicFunctionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleInterpolatedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleInterpolatedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onIwasawaTheoryInterpolated?.RegisterCallback(_handleInterpolatedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onIwasawaTheoryInterpolated?.UnregisterCallback(_handleInterpolatedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_iwasawaTheorySO == null) return;
            int bonus = _iwasawaTheorySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_iwasawaTheorySO == null) return;
            _iwasawaTheorySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _iwasawaTheorySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_iwasawaTheorySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_padicFunctionLabel != null)
                _padicFunctionLabel.text = $"p-Adic Functions: {_iwasawaTheorySO.PadicFunctions}/{_iwasawaTheorySO.PadicFunctionsNeeded}";

            if (_interpolationLabel != null)
                _interpolationLabel.text = $"Interpolations: {_iwasawaTheorySO.InterpolationCount}";

            if (_padicFunctionBar != null)
                _padicFunctionBar.value = _iwasawaTheorySO.PadicFunctionProgress;
        }

        public ZoneControlCaptureIwasawaTheorySO IwasawaTheorySO => _iwasawaTheorySO;
    }
}

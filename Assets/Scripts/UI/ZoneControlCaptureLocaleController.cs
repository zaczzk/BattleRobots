using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLocaleController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLocaleSO _localeSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLocaleCovered;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _openLabel;
        [SerializeField] private Text       _coverCountLabel;
        [SerializeField] private Slider     _openBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleLocaleCoveredDelegate;

        private void Awake()
        {
            _handlePlayerDelegate        = HandlePlayerCaptured;
            _handleBotDelegate           = HandleBotCaptured;
            _handleMatchStartedDelegate  = HandleMatchStarted;
            _handleLocaleCoveredDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLocaleCovered?.RegisterCallback(_handleLocaleCoveredDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLocaleCovered?.UnregisterCallback(_handleLocaleCoveredDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_localeSO == null) return;
            int bonus = _localeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_localeSO == null) return;
            _localeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _localeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_localeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_openLabel != null)
                _openLabel.text = $"Opens: {_localeSO.Opens}/{_localeSO.OpensNeeded}";

            if (_coverCountLabel != null)
                _coverCountLabel.text = $"Coverings: {_localeSO.CoverCount}";

            if (_openBar != null)
                _openBar.value = _localeSO.OpenProgress;
        }

        public ZoneControlCaptureLocaleSO LocaleSO => _localeSO;
    }
}

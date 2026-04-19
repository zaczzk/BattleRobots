using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureEclipseController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureEclipseSO _eclipseSO;
        [SerializeField] private PlayerWalletSO              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onEclipseStarted;
        [SerializeField] private VoidGameEvent _onEclipseEnded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _statusLabel;
        [SerializeField] private Text       _eclipseCapturesLabel;
        [SerializeField] private Text       _bonusLabel;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleEclipseStartedDelegate;
        private Action _handleEclipseEndedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate         = HandlePlayerCaptured;
            _handleBotDelegate            = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleEclipseStartedDelegate = Refresh;
            _handleEclipseEndedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onEclipseStarted?.RegisterCallback(_handleEclipseStartedDelegate);
            _onEclipseEnded?.RegisterCallback(_handleEclipseEndedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onEclipseStarted?.UnregisterCallback(_handleEclipseStartedDelegate);
            _onEclipseEnded?.UnregisterCallback(_handleEclipseEndedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_eclipseSO == null) return;
            bool wasEclipsed = _eclipseSO.IsEclipsed;
            _eclipseSO.RecordPlayerCapture();
            if (wasEclipsed)
                _wallet?.AddFunds(_eclipseSO.BonusPerEclipseCapture);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_eclipseSO == null) return;
            _eclipseSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _eclipseSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_eclipseSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_statusLabel != null)
                _statusLabel.text = _eclipseSO.IsEclipsed ? "ECLIPSE!" : "Clear";

            if (_eclipseCapturesLabel != null)
                _eclipseCapturesLabel.text = $"Eclipse Captures: {_eclipseSO.EclipseCaptures}";

            if (_bonusLabel != null)
                _bonusLabel.text = $"Eclipse Bonus: {_eclipseSO.TotalBonusAwarded}";
        }

        public ZoneControlCaptureEclipseSO EclipseSO => _eclipseSO;
    }
}

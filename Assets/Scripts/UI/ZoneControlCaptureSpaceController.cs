using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSpaceController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSpaceSO _spaceSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onSpaceOpened;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _pointLabel;
        [SerializeField] private Text       _openLabel;
        [SerializeField] private Slider     _pointBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleOpenedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleOpenedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onSpaceOpened?.RegisterCallback(_handleOpenedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onSpaceOpened?.UnregisterCallback(_handleOpenedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_spaceSO == null) return;
            int bonus = _spaceSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_spaceSO == null) return;
            _spaceSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _spaceSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_spaceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_pointLabel != null)
                _pointLabel.text = $"Points: {_spaceSO.Points}/{_spaceSO.PointsNeeded}";

            if (_openLabel != null)
                _openLabel.text = $"Opens: {_spaceSO.OpenCount}";

            if (_pointBar != null)
                _pointBar.value = _spaceSO.SpaceProgress;
        }

        public ZoneControlCaptureSpaceSO SpaceSO => _spaceSO;
    }
}

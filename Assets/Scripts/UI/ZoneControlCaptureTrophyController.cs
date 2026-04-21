using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTrophyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTrophySO _trophySO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTrophyAwarded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _medalLabel;
        [SerializeField] private Text       _trophyLabel;
        [SerializeField] private Slider     _medalBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTrophyAwardedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleTrophyAwardedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTrophyAwarded?.RegisterCallback(_handleTrophyAwardedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTrophyAwarded?.UnregisterCallback(_handleTrophyAwardedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_trophySO == null) return;
            int bonus = _trophySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_trophySO == null) return;
            _trophySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _trophySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_trophySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_medalLabel != null)
                _medalLabel.text = $"Medals: {_trophySO.Medals}/{_trophySO.MedalsNeeded}";

            if (_trophyLabel != null)
                _trophyLabel.text = $"Trophies: {_trophySO.TrophyCount}";

            if (_medalBar != null)
                _medalBar.value = _trophySO.MedalProgress;
        }

        public ZoneControlCaptureTrophySO TrophySO => _trophySO;
    }
}

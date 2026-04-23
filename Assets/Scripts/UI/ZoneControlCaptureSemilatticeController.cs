using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureSemilatticeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureSemilatticeSO _semilatticeSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMeetFormed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _meetLabel;
        [SerializeField] private Text       _meetCountLabel;
        [SerializeField] private Slider     _meetBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMeetFormedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMeetFormedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMeetFormed?.RegisterCallback(_handleMeetFormedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMeetFormed?.UnregisterCallback(_handleMeetFormedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_semilatticeSO == null) return;
            int bonus = _semilatticeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_semilatticeSO == null) return;
            _semilatticeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _semilatticeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_semilatticeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_meetLabel != null)
                _meetLabel.text = $"Meets: {_semilatticeSO.Meets}/{_semilatticeSO.MeetsNeeded}";

            if (_meetCountLabel != null)
                _meetCountLabel.text = $"Meet-Joins: {_semilatticeSO.MeetCount}";

            if (_meetBar != null)
                _meetBar.value = _semilatticeSO.MeetProgress;
        }

        public ZoneControlCaptureSemilatticeSO SemilatticeSO => _semilatticeSO;
    }
}

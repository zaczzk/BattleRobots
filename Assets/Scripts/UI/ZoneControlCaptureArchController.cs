using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureArchController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureArchSO _archSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onArchComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _keystoneLabel;
        [SerializeField] private Text       _archLabel;
        [SerializeField] private Slider     _keystoneBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleArchCompleteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleArchCompleteDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onArchComplete?.RegisterCallback(_handleArchCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onArchComplete?.UnregisterCallback(_handleArchCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_archSO == null) return;
            int bonus = _archSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_archSO == null) return;
            _archSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _archSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_archSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_keystoneLabel != null)
                _keystoneLabel.text = $"Keystones: {_archSO.Keystones}/{_archSO.KeystonesNeeded}";

            if (_archLabel != null)
                _archLabel.text = $"Arches: {_archSO.ArchCount}";

            if (_keystoneBar != null)
                _keystoneBar.value = _archSO.KeystoneProgress;
        }

        public ZoneControlCaptureArchSO ArchSO => _archSO;
    }
}

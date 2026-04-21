using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLoomController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLoomSO _loomSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLoomWoven;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _threadLabel;
        [SerializeField] private Text       _weaveLabel;
        [SerializeField] private Slider     _threadBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleWovenDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleWovenDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLoomWoven?.RegisterCallback(_handleWovenDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLoomWoven?.UnregisterCallback(_handleWovenDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_loomSO == null) return;
            int bonus = _loomSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_loomSO == null) return;
            _loomSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _loomSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_loomSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_threadLabel != null)
                _threadLabel.text = $"Threads: {_loomSO.Threads}/{_loomSO.ThreadsNeeded}";

            if (_weaveLabel != null)
                _weaveLabel.text = $"Weaves: {_loomSO.WeaveCount}";

            if (_threadBar != null)
                _threadBar.value = _loomSO.ThreadProgress;
        }

        public ZoneControlCaptureLoomSO LoomSO => _loomSO;
    }
}

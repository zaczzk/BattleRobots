using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDeloopingController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDeloopingSO _deloopingSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDeloopingComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _loopLabel;
        [SerializeField] private Text       _deloopLabel;
        [SerializeField] private Slider     _loopBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDeloopDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDeloopDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDeloopingComplete?.RegisterCallback(_handleDeloopDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDeloopingComplete?.UnregisterCallback(_handleDeloopDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_deloopingSO == null) return;
            int bonus = _deloopingSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_deloopingSO == null) return;
            _deloopingSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _deloopingSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_deloopingSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_loopLabel != null)
                _loopLabel.text = $"Loops: {_deloopingSO.Loops}/{_deloopingSO.LoopsNeeded}";

            if (_deloopLabel != null)
                _deloopLabel.text = $"Deloopings: {_deloopingSO.DeloopCount}";

            if (_loopBar != null)
                _loopBar.value = _deloopingSO.LoopProgress;
        }

        public ZoneControlCaptureDeloopingSO DeloopingSO => _deloopingSO;
    }
}

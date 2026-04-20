using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePhoenixController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePhoenixSO _phoenixSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPhoenixReborn;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _ashLabel;
        [SerializeField] private Text       _rebirthLabel;
        [SerializeField] private Slider     _ashBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleRebornDelegate;

        private void Awake()
        {
            _handlePlayerDelegate      = HandlePlayerCaptured;
            _handleBotDelegate         = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleRebornDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPhoenixReborn?.RegisterCallback(_handleRebornDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPhoenixReborn?.UnregisterCallback(_handleRebornDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_phoenixSO == null) return;
            int bonus = _phoenixSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_phoenixSO == null) return;
            _phoenixSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _phoenixSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_phoenixSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_ashLabel != null)
                _ashLabel.text = $"Ashes: {_phoenixSO.Ashes}/{_phoenixSO.AshesNeeded}";

            if (_rebirthLabel != null)
                _rebirthLabel.text = $"Rebirths: {_phoenixSO.RebirthCount}";

            if (_ashBar != null)
                _ashBar.value = _phoenixSO.AshProgress;
        }

        public ZoneControlCapturePhoenixSO PhoenixSO => _phoenixSO;
    }
}

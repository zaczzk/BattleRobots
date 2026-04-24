using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureOpcartController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureOpcartSO _opcartSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onOpcartLifted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _liftLabel;
        [SerializeField] private Text       _opcartLabel;
        [SerializeField] private Slider     _liftBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleLiftedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleLiftedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onOpcartLifted?.RegisterCallback(_handleLiftedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onOpcartLifted?.UnregisterCallback(_handleLiftedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_opcartSO == null) return;
            int bonus = _opcartSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_opcartSO == null) return;
            _opcartSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _opcartSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_opcartSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_liftLabel != null)
                _liftLabel.text = $"Lifts: {_opcartSO.Lifts}/{_opcartSO.LiftsNeeded}";

            if (_opcartLabel != null)
                _opcartLabel.text = $"Opcarts: {_opcartSO.LiftCount}";

            if (_liftBar != null)
                _liftBar.value = _opcartSO.LiftProgress;
        }

        public ZoneControlCaptureOpcartSO OpcartSO => _opcartSO;
    }
}

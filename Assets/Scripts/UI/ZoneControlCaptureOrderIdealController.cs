using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureOrderIdealController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureOrderIdealSO _orderIdealSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onOrderIdealExtended;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _idealLabel;
        [SerializeField] private Text       _extendLabel;
        [SerializeField] private Slider     _idealBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleExtendedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleExtendedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onOrderIdealExtended?.RegisterCallback(_handleExtendedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onOrderIdealExtended?.UnregisterCallback(_handleExtendedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_orderIdealSO == null) return;
            int bonus = _orderIdealSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_orderIdealSO == null) return;
            _orderIdealSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _orderIdealSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_orderIdealSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_idealLabel != null)
                _idealLabel.text = $"Ideals: {_orderIdealSO.Ideals}/{_orderIdealSO.IdealsNeeded}";

            if (_extendLabel != null)
                _extendLabel.text = $"Extensions: {_orderIdealSO.ExtensionCount}";

            if (_idealBar != null)
                _idealBar.value = _orderIdealSO.IdealProgress;
        }

        public ZoneControlCaptureOrderIdealSO OrderIdealSO => _orderIdealSO;
    }
}

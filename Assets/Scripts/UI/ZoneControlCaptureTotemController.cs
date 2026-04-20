using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTotemController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTotemSO _totemSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTotemRaised;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _ringLabel;
        [SerializeField] private Text       _totemLabel;
        [SerializeField] private Slider     _ringBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTotemRaisedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleTotemRaisedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTotemRaised?.RegisterCallback(_handleTotemRaisedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTotemRaised?.UnregisterCallback(_handleTotemRaisedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_totemSO == null) return;
            int bonus = _totemSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_totemSO == null) return;
            _totemSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _totemSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_totemSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_ringLabel != null)
                _ringLabel.text = $"Rings: {_totemSO.Rings}/{_totemSO.RingsNeeded}";

            if (_totemLabel != null)
                _totemLabel.text = $"Totems: {_totemSO.TotemCount}";

            if (_ringBar != null)
                _ringBar.value = _totemSO.RingProgress;
        }

        public ZoneControlCaptureTotemSO TotemSO => _totemSO;
    }
}

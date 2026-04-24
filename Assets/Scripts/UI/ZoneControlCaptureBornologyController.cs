using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBornologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBornologySO _bornologySO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onBornologyBounded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _setLabel;
        [SerializeField] private Text       _boundLabel;
        [SerializeField] private Slider     _setBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBoundedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleBoundedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onBornologyBounded?.RegisterCallback(_handleBoundedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onBornologyBounded?.UnregisterCallback(_handleBoundedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_bornologySO == null) return;
            int bonus = _bornologySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_bornologySO == null) return;
            _bornologySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _bornologySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_bornologySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_setLabel != null)
                _setLabel.text = $"Sets: {_bornologySO.Sets}/{_bornologySO.SetsNeeded}";

            if (_boundLabel != null)
                _boundLabel.text = $"Bounds: {_bornologySO.BoundCount}";

            if (_setBar != null)
                _setBar.value = _bornologySO.BornologyProgress;
        }

        public ZoneControlCaptureBornologySO BornologySO => _bornologySO;
    }
}

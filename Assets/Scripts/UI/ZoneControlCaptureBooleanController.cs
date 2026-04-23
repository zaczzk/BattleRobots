using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureBooleanController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureBooleanSO _booleanSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onComplemented;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _complementLabel;
        [SerializeField] private Text       _complementCountLabel;
        [SerializeField] private Slider     _complementBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleComplementedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleComplementedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onComplemented?.RegisterCallback(_handleComplementedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onComplemented?.UnregisterCallback(_handleComplementedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_booleanSO == null) return;
            int bonus = _booleanSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_booleanSO == null) return;
            _booleanSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _booleanSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_booleanSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_complementLabel != null)
                _complementLabel.text = $"Complements: {_booleanSO.Complements}/{_booleanSO.ComplementsNeeded}";

            if (_complementCountLabel != null)
                _complementCountLabel.text = $"Complementations: {_booleanSO.ComplementCount}";

            if (_complementBar != null)
                _complementBar.value = _booleanSO.ComplementProgress;
        }

        public ZoneControlCaptureBooleanSO BooleanSO => _booleanSO;
    }
}

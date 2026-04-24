using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCobordismController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCobordismSO _cobordismSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCobordismComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _boundaryLabel;
        [SerializeField] private Text       _cobordLabel;
        [SerializeField] private Slider     _boundaryBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCobordismDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCobordismDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCobordismComplete?.RegisterCallback(_handleCobordismDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCobordismComplete?.UnregisterCallback(_handleCobordismDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cobordismSO == null) return;
            int bonus = _cobordismSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cobordismSO == null) return;
            _cobordismSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cobordismSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cobordismSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_boundaryLabel != null)
                _boundaryLabel.text = $"Boundaries: {_cobordismSO.Boundaries}/{_cobordismSO.BoundariesNeeded}";

            if (_cobordLabel != null)
                _cobordLabel.text = $"Cobordisms: {_cobordismSO.CobordismCount}";

            if (_boundaryBar != null)
                _boundaryBar.value = _cobordismSO.CobordismProgress;
        }

        public ZoneControlCaptureCobordismSO CobordismSO => _cobordismSO;
    }
}

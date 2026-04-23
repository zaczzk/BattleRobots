using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureHeytingController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureHeytingSO _heytingSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onImplicationFormed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _implicationLabel;
        [SerializeField] private Text       _implyCountLabel;
        [SerializeField] private Slider     _implicationBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleImplicationDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleImplicationDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onImplicationFormed?.RegisterCallback(_handleImplicationDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onImplicationFormed?.UnregisterCallback(_handleImplicationDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_heytingSO == null) return;
            int bonus = _heytingSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_heytingSO == null) return;
            _heytingSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _heytingSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_heytingSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_implicationLabel != null)
                _implicationLabel.text = $"Impls: {_heytingSO.Implications}/{_heytingSO.ImplicationsNeeded}";

            if (_implyCountLabel != null)
                _implyCountLabel.text = $"Implications: {_heytingSO.ImplicationCount}";

            if (_implicationBar != null)
                _implicationBar.value = _heytingSO.ImplicationProgress;
        }

        public ZoneControlCaptureHeytingSO HeytingSO => _heytingSO;
    }
}

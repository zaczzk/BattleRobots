using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureForestController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureForestSO _forestSO;
        [SerializeField] private PlayerWallet               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onForestFlourished;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _treeLabel;
        [SerializeField] private Text       _flourishLabel;
        [SerializeField] private Slider     _treeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFlourishedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate      = HandlePlayerCaptured;
            _handleBotDelegate         = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFlourishedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onForestFlourished?.RegisterCallback(_handleFlourishedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onForestFlourished?.UnregisterCallback(_handleFlourishedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_forestSO == null) return;
            int bonus = _forestSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_forestSO == null) return;
            _forestSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _forestSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_forestSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_treeLabel != null)
                _treeLabel.text = $"Trees: {_forestSO.Trees}/{_forestSO.TreesNeeded}";

            if (_flourishLabel != null)
                _flourishLabel.text = $"Flourishes: {_forestSO.FlourishCount}";

            if (_treeBar != null)
                _treeBar.value = _forestSO.TreeProgress;
        }

        public ZoneControlCaptureForestSO ForestSO => _forestSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureNodeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureNodeSO _nodeSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onNodeChained;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _linkLabel;
        [SerializeField] private Text       _chainLabel;
        [SerializeField] private Slider     _linkBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleChainedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleChainedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onNodeChained?.RegisterCallback(_handleChainedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onNodeChained?.UnregisterCallback(_handleChainedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_nodeSO == null) return;
            int bonus = _nodeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_nodeSO == null) return;
            _nodeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _nodeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_nodeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_linkLabel != null)
                _linkLabel.text = $"Links: {_nodeSO.Links}/{_nodeSO.LinksNeeded}";

            if (_chainLabel != null)
                _chainLabel.text = $"Chains: {_nodeSO.ChainCount}";

            if (_linkBar != null)
                _linkBar.value = _nodeSO.LinkProgress;
        }

        public ZoneControlCaptureNodeSO NodeSO => _nodeSO;
    }
}

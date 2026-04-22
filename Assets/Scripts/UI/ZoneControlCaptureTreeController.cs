using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTreeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTreeSO _treeSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTreeGrown;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _nodeLabel;
        [SerializeField] private Text       _growLabel;
        [SerializeField] private Slider     _nodeBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleGrownDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleGrownDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTreeGrown?.RegisterCallback(_handleGrownDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTreeGrown?.UnregisterCallback(_handleGrownDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_treeSO == null) return;
            int bonus = _treeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_treeSO == null) return;
            _treeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _treeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_treeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_nodeLabel != null)
                _nodeLabel.text = $"Nodes: {_treeSO.Nodes}/{_treeSO.NodesNeeded}";

            if (_growLabel != null)
                _growLabel.text = $"Growths: {_treeSO.GrowCount}";

            if (_nodeBar != null)
                _nodeBar.value = _treeSO.NodeProgress;
        }

        public ZoneControlCaptureTreeSO TreeSO => _treeSO;
    }
}

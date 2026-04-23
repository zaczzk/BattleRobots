using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTraversalController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTraversalSO _traversalSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTraversalComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _elementLabel;
        [SerializeField] private Text       _traverseLabel;
        [SerializeField] private Slider     _elementBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleTraversalDelegate;

        private void Awake()
        {
            _handlePlayerDelegate    = HandlePlayerCaptured;
            _handleBotDelegate       = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleTraversalDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTraversalComplete?.RegisterCallback(_handleTraversalDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTraversalComplete?.UnregisterCallback(_handleTraversalDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_traversalSO == null) return;
            int bonus = _traversalSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_traversalSO == null) return;
            _traversalSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _traversalSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_traversalSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_elementLabel != null)
                _elementLabel.text = $"Elements: {_traversalSO.Elements}/{_traversalSO.ElementsNeeded}";

            if (_traverseLabel != null)
                _traverseLabel.text = $"Traversals: {_traversalSO.TraverseCount}";

            if (_elementBar != null)
                _elementBar.value = _traversalSO.ElementProgress;
        }

        public ZoneControlCaptureTraversalSO TraversalSO => _traversalSO;
    }
}

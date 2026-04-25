using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureGrothendieckRingController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureGrothendieckRingSO _grothendieckRingSO;
        [SerializeField] private PlayerWallet                          _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onGrothendieckRingAdded;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _virtualObjectLabel;
        [SerializeField] private Text       _addLabel;
        [SerializeField] private Slider     _virtualObjectBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleAddedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleAddedDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onGrothendieckRingAdded?.RegisterCallback(_handleAddedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onGrothendieckRingAdded?.UnregisterCallback(_handleAddedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_grothendieckRingSO == null) return;
            int bonus = _grothendieckRingSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_grothendieckRingSO == null) return;
            _grothendieckRingSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _grothendieckRingSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_grothendieckRingSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_virtualObjectLabel != null)
                _virtualObjectLabel.text = $"Virtual Objects: {_grothendieckRingSO.VirtualObjects}/{_grothendieckRingSO.VirtualObjectsNeeded}";

            if (_addLabel != null)
                _addLabel.text = $"Additions: {_grothendieckRingSO.AdditionCount}";

            if (_virtualObjectBar != null)
                _virtualObjectBar.value = _grothendieckRingSO.VirtualObjectProgress;
        }

        public ZoneControlCaptureGrothendieckRingSO GrothendieckRingSO => _grothendieckRingSO;
    }
}

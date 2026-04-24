using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureInfinityToposController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureInfinityToposSO _infinityToposSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onInfinityToposDescended;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _descentLabel;
        [SerializeField] private Text       _descendCountLabel;
        [SerializeField] private Slider     _descentBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDescendedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDescendedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onInfinityToposDescended?.RegisterCallback(_handleDescendedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onInfinityToposDescended?.UnregisterCallback(_handleDescendedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_infinityToposSO == null) return;
            int bonus = _infinityToposSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_infinityToposSO == null) return;
            _infinityToposSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _infinityToposSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_infinityToposSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_descentLabel != null)
                _descentLabel.text = $"Descents: {_infinityToposSO.DescentConditions}/{_infinityToposSO.DescentConditionsNeeded}";

            if (_descendCountLabel != null)
                _descendCountLabel.text = $"Descendings: {_infinityToposSO.DescendCount}";

            if (_descentBar != null)
                _descentBar.value = _infinityToposSO.DescentProgress;
        }

        public ZoneControlCaptureInfinityToposSO InfinityToposSO => _infinityToposSO;
    }
}

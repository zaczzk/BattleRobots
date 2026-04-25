using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureStringTopologyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureStringTopologySO _stringTopologySO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onStringTopologyIntersected;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _loopLabel;
        [SerializeField] private Text       _intersectLabel;
        [SerializeField] private Slider     _loopBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleIntersectedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleIntersectedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onStringTopologyIntersected?.RegisterCallback(_handleIntersectedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onStringTopologyIntersected?.UnregisterCallback(_handleIntersectedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_stringTopologySO == null) return;
            int bonus = _stringTopologySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_stringTopologySO == null) return;
            _stringTopologySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _stringTopologySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_stringTopologySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_loopLabel != null)
                _loopLabel.text = $"Loops: {_stringTopologySO.Loops}/{_stringTopologySO.LoopsNeeded}";

            if (_intersectLabel != null)
                _intersectLabel.text = $"Intersections: {_stringTopologySO.IntersectionCount}";

            if (_loopBar != null)
                _loopBar.value = _stringTopologySO.LoopProgress;
        }

        public ZoneControlCaptureStringTopologySO StringTopologySO => _stringTopologySO;
    }
}

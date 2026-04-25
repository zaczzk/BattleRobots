using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureChowGroupController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureChowGroupSO _chowGroupSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onChowGroupIntersected;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _rationalCycleLabel;
        [SerializeField] private Text       _intersectLabel;
        [SerializeField] private Slider     _rationalCycleBar;
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
            _onChowGroupIntersected?.RegisterCallback(_handleIntersectedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onChowGroupIntersected?.UnregisterCallback(_handleIntersectedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_chowGroupSO == null) return;
            int bonus = _chowGroupSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_chowGroupSO == null) return;
            _chowGroupSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _chowGroupSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_chowGroupSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_rationalCycleLabel != null)
                _rationalCycleLabel.text = $"Rational Cycles: {_chowGroupSO.RationalCycles}/{_chowGroupSO.RationalCyclesNeeded}";

            if (_intersectLabel != null)
                _intersectLabel.text = $"Intersections: {_chowGroupSO.IntersectionCount}";

            if (_rationalCycleBar != null)
                _rationalCycleBar.value = _chowGroupSO.RationalCycleProgress;
        }

        public ZoneControlCaptureChowGroupSO ChowGroupSO => _chowGroupSO;
    }
}

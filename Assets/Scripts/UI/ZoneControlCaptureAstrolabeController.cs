using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureAstrolabeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureAstrolabeSO _astrolabeSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onAstrolabeAligned;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _chartingLabel;
        [SerializeField] private Text       _alignmentLabel;
        [SerializeField] private Slider     _chartingBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleAlignedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleAlignedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onAstrolabeAligned?.RegisterCallback(_handleAlignedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onAstrolabeAligned?.UnregisterCallback(_handleAlignedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_astrolabeSO == null) return;
            int bonus = _astrolabeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_astrolabeSO == null) return;
            _astrolabeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _astrolabeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_astrolabeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_chartingLabel != null)
                _chartingLabel.text = $"Chartings: {_astrolabeSO.Chartings}/{_astrolabeSO.ChartingsNeeded}";

            if (_alignmentLabel != null)
                _alignmentLabel.text = $"Alignments: {_astrolabeSO.AlignmentCount}";

            if (_chartingBar != null)
                _chartingBar.value = _astrolabeSO.ChartingProgress;
        }

        public ZoneControlCaptureAstrolabeSO AstrolabeSO => _astrolabeSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePyramidController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePyramidSO _pyramidSO;
        [SerializeField] private PlayerWalletSO               _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPyramidComplete;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _tierLabel;
        [SerializeField] private Text       _pyramidCountLabel;
        [SerializeField] private Slider     _tierBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handlePyramidCompleteDelegate;

        private void Awake()
        {
            _handlePlayerDelegate          = HandlePlayerCaptured;
            _handleBotDelegate             = HandleBotCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handlePyramidCompleteDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPyramidComplete?.RegisterCallback(_handlePyramidCompleteDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPyramidComplete?.UnregisterCallback(_handlePyramidCompleteDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_pyramidSO == null) return;
            int prev  = _pyramidSO.PyramidCount;
            int bonus = _pyramidSO.RecordPlayerCapture();
            if (_pyramidSO.PyramidCount > prev)
                _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_pyramidSO == null) return;
            _pyramidSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _pyramidSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_pyramidSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_tierLabel != null)
                _tierLabel.text = $"Tier: {_pyramidSO.CurrentTier}/{_pyramidSO.MaxTiers}";

            if (_pyramidCountLabel != null)
                _pyramidCountLabel.text = $"Pyramids: {_pyramidSO.PyramidCount}";

            if (_tierBar != null)
                _tierBar.value = _pyramidSO.TierProgress;
        }

        public ZoneControlCapturePyramidSO PyramidSO => _pyramidSO;
    }
}

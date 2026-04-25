using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureShimuraVarietyController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureShimuraVarietySO _shimuraVarietySO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onShimuraVarietyUniformized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _cmPointLabel;
        [SerializeField] private Text       _uniformizationLabel;
        [SerializeField] private Slider     _cmPointBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleUniformizedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleUniformizedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onShimuraVarietyUniformized?.RegisterCallback(_handleUniformizedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onShimuraVarietyUniformized?.UnregisterCallback(_handleUniformizedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_shimuraVarietySO == null) return;
            int bonus = _shimuraVarietySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_shimuraVarietySO == null) return;
            _shimuraVarietySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _shimuraVarietySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_shimuraVarietySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_cmPointLabel != null)
                _cmPointLabel.text = $"CM Points: {_shimuraVarietySO.CMPoints}/{_shimuraVarietySO.CMPointsNeeded}";

            if (_uniformizationLabel != null)
                _uniformizationLabel.text = $"Uniformizations: {_shimuraVarietySO.UniformizationCount}";

            if (_cmPointBar != null)
                _cmPointBar.value = _shimuraVarietySO.CMPointProgress;
        }

        public ZoneControlCaptureShimuraVarietySO ShimuraVarietySO => _shimuraVarietySO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePipelineController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePipelineSO _pipelineSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPipelineFlushed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _stageLabel;
        [SerializeField] private Text       _flushLabel;
        [SerializeField] private Slider     _stageBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFlushedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFlushedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPipelineFlushed?.RegisterCallback(_handleFlushedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPipelineFlushed?.UnregisterCallback(_handleFlushedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_pipelineSO == null) return;
            int bonus = _pipelineSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_pipelineSO == null) return;
            _pipelineSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _pipelineSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_pipelineSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_stageLabel != null)
                _stageLabel.text = $"Stages: {_pipelineSO.Stages}/{_pipelineSO.StagesNeeded}";

            if (_flushLabel != null)
                _flushLabel.text = $"Flushes: {_pipelineSO.FlushCount}";

            if (_stageBar != null)
                _stageBar.value = _pipelineSO.StageProgress;
        }

        public ZoneControlCapturePipelineSO PipelineSO => _pipelineSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureMordellWeilController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureMordellWeilSO _mordellWeilSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onMordellWeilGenerated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _generatorLabel;
        [SerializeField] private Text       _generationLabel;
        [SerializeField] private Slider     _generatorBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleGeneratedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleGeneratedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onMordellWeilGenerated?.RegisterCallback(_handleGeneratedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onMordellWeilGenerated?.UnregisterCallback(_handleGeneratedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_mordellWeilSO == null) return;
            int bonus = _mordellWeilSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_mordellWeilSO == null) return;
            _mordellWeilSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _mordellWeilSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_mordellWeilSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_generatorLabel != null)
                _generatorLabel.text = $"Generators: {_mordellWeilSO.Generators}/{_mordellWeilSO.GeneratorsNeeded}";

            if (_generationLabel != null)
                _generationLabel.text = $"Generations: {_mordellWeilSO.GenerationCount}";

            if (_generatorBar != null)
                _generatorBar.value = _mordellWeilSO.GeneratorProgress;
        }

        public ZoneControlCaptureMordellWeilSO MordellWeilSO => _mordellWeilSO;
    }
}

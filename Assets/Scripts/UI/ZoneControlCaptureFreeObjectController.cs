using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFreeObjectController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFreeObjectSO _freeObjectSO;
        [SerializeField] private PlayerWallet                    _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFreeObjectGenerated;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _generatorLabel;
        [SerializeField] private Text       _freeObjectLabel;
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
            _onFreeObjectGenerated?.RegisterCallback(_handleGeneratedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFreeObjectGenerated?.UnregisterCallback(_handleGeneratedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_freeObjectSO == null) return;
            int bonus = _freeObjectSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_freeObjectSO == null) return;
            _freeObjectSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _freeObjectSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_freeObjectSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_generatorLabel != null)
                _generatorLabel.text = $"Generators: {_freeObjectSO.Generators}/{_freeObjectSO.GeneratorsNeeded}";

            if (_freeObjectLabel != null)
                _freeObjectLabel.text = $"Free Objects: {_freeObjectSO.FreeObjectCount}";

            if (_generatorBar != null)
                _generatorBar.value = _freeObjectSO.GeneratorProgress;
        }

        public ZoneControlCaptureFreeObjectSO FreeObjectSO => _freeObjectSO;
    }
}

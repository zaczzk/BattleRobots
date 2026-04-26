using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureModalLogicController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureModalLogicSO _modalLogicSO;
        [SerializeField] private PlayerWallet                   _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onModalLogicCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _possibleWorldLabel;
        [SerializeField] private Text       _completionCountLabel;
        [SerializeField] private Slider     _possibleWorldBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCompletedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCompletedDelegate    = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onModalLogicCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onModalLogicCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_modalLogicSO == null) return;
            int bonus = _modalLogicSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_modalLogicSO == null) return;
            _modalLogicSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _modalLogicSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_modalLogicSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_possibleWorldLabel != null)
                _possibleWorldLabel.text =
                    $"Possible Worlds: {_modalLogicSO.PossibleWorlds}/{_modalLogicSO.PossibleWorldsNeeded}";

            if (_completionCountLabel != null)
                _completionCountLabel.text = $"Completions: {_modalLogicSO.CompletionCount}";

            if (_possibleWorldBar != null)
                _possibleWorldBar.value = _modalLogicSO.PossibleWorldProgress;
        }

        public ZoneControlCaptureModalLogicSO ModalLogicSO => _modalLogicSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureConcurrentTypeController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureConcurrentTypeSO _concurrentTypeSO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onConcurrentTypeCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _raceFreeDerivationLabel;
        [SerializeField] private Text       _derivationCountLabel;
        [SerializeField] private Slider     _raceFreeDerivationBar;
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
            _onConcurrentTypeCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onConcurrentTypeCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_concurrentTypeSO == null) return;
            int bonus = _concurrentTypeSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_concurrentTypeSO == null) return;
            _concurrentTypeSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _concurrentTypeSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_concurrentTypeSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_raceFreeDerivationLabel != null)
                _raceFreeDerivationLabel.text =
                    $"Race-Free Derivations: {_concurrentTypeSO.RaceFreeDerivations}/{_concurrentTypeSO.RaceFreeDerivationsNeeded}";

            if (_derivationCountLabel != null)
                _derivationCountLabel.text = $"Derivations: {_concurrentTypeSO.DerivationCount}";

            if (_raceFreeDerivationBar != null)
                _raceFreeDerivationBar.value = _concurrentTypeSO.RaceFreeDerivationProgress;
        }

        public ZoneControlCaptureConcurrentTypeSO ConcurrentTypeSO => _concurrentTypeSO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCutEliminationController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCutEliminationSO _cutEliminationSO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCutEliminationAchieved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _cutFreeDerivationLabel;
        [SerializeField] private Text       _eliminationCountLabel;
        [SerializeField] private Slider     _cutFreeDerivationBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleAchievedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleAchievedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCutEliminationAchieved?.RegisterCallback(_handleAchievedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCutEliminationAchieved?.UnregisterCallback(_handleAchievedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cutEliminationSO == null) return;
            int bonus = _cutEliminationSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cutEliminationSO == null) return;
            _cutEliminationSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cutEliminationSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cutEliminationSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_cutFreeDerivationLabel != null)
                _cutFreeDerivationLabel.text =
                    $"Cut-Free Derivations: {_cutEliminationSO.CutFreeDerivations}/{_cutEliminationSO.CutFreeDerivationsNeeded}";

            if (_eliminationCountLabel != null)
                _eliminationCountLabel.text = $"Eliminations: {_cutEliminationSO.EliminationCount}";

            if (_cutFreeDerivationBar != null)
                _cutFreeDerivationBar.value = _cutEliminationSO.CutFreeDerivationProgress;
        }

        public ZoneControlCaptureCutEliminationSO CutEliminationSO => _cutEliminationSO;
    }
}

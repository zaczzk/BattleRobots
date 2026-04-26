using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureResolutionPrincipleController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureResolutionPrincipleSO _resolutionPrincipleSO;
        [SerializeField] private PlayerWallet                            _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onResolutionPrincipleApplied;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _resolvedClauseLabel;
        [SerializeField] private Text       _resolutionCountLabel;
        [SerializeField] private Slider     _resolvedClauseBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleAppliedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleAppliedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onResolutionPrincipleApplied?.RegisterCallback(_handleAppliedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onResolutionPrincipleApplied?.UnregisterCallback(_handleAppliedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_resolutionPrincipleSO == null) return;
            int bonus = _resolutionPrincipleSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_resolutionPrincipleSO == null) return;
            _resolutionPrincipleSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _resolutionPrincipleSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_resolutionPrincipleSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_resolvedClauseLabel != null)
                _resolvedClauseLabel.text =
                    $"Resolved Clauses: {_resolutionPrincipleSO.ResolvedClauses}/{_resolutionPrincipleSO.ResolvedClausesNeeded}";

            if (_resolutionCountLabel != null)
                _resolutionCountLabel.text = $"Resolutions: {_resolutionPrincipleSO.ResolutionCount}";

            if (_resolvedClauseBar != null)
                _resolvedClauseBar.value = _resolutionPrincipleSO.ResolvedClauseProgress;
        }

        public ZoneControlCaptureResolutionPrincipleSO ResolutionPrincipleSO => _resolutionPrincipleSO;
    }
}

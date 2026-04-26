using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureRefinementTypesController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureRefinementTypesSO _refinementTypesSO;
        [SerializeField] private PlayerWallet                         _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onRefinementTypesCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _verifiedRefinementLabel;
        [SerializeField] private Text       _refinementCountLabel;
        [SerializeField] private Slider     _verifiedRefinementBar;
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
            _onRefinementTypesCompleted?.RegisterCallback(_handleCompletedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onRefinementTypesCompleted?.UnregisterCallback(_handleCompletedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_refinementTypesSO == null) return;
            int bonus = _refinementTypesSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_refinementTypesSO == null) return;
            _refinementTypesSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _refinementTypesSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_refinementTypesSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_verifiedRefinementLabel != null)
                _verifiedRefinementLabel.text =
                    $"Verified Refinements: {_refinementTypesSO.VerifiedRefinements}/{_refinementTypesSO.VerifiedRefinementsNeeded}";

            if (_refinementCountLabel != null)
                _refinementCountLabel.text = $"Refinements: {_refinementTypesSO.RefinementCount}";

            if (_verifiedRefinementBar != null)
                _verifiedRefinementBar.value = _refinementTypesSO.VerifiedRefinementProgress;
        }

        public ZoneControlCaptureRefinementTypesSO RefinementTypesSO => _refinementTypesSO;
    }
}

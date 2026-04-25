using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureZermeloFraenkelController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureZermeloFraenkelSO _zermeloFraenkelSO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onZermeloFraenkelVerified;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _verifiedAxiomLabel;
        [SerializeField] private Text       _axiomSetCountLabel;
        [SerializeField] private Slider     _axiomBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleVerifiedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleVerifiedDelegate     = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onZermeloFraenkelVerified?.RegisterCallback(_handleVerifiedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onZermeloFraenkelVerified?.UnregisterCallback(_handleVerifiedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_zermeloFraenkelSO == null) return;
            int bonus = _zermeloFraenkelSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_zermeloFraenkelSO == null) return;
            _zermeloFraenkelSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _zermeloFraenkelSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_zermeloFraenkelSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_verifiedAxiomLabel != null)
                _verifiedAxiomLabel.text =
                    $"Verified Axioms: {_zermeloFraenkelSO.VerifiedAxioms}/{_zermeloFraenkelSO.VerifiedAxiomsNeeded}";

            if (_axiomSetCountLabel != null)
                _axiomSetCountLabel.text = $"Axiom Sets: {_zermeloFraenkelSO.AxiomSetCount}";

            if (_axiomBar != null)
                _axiomBar.value = _zermeloFraenkelSO.VerifiedAxiomProgress;
        }

        public ZoneControlCaptureZermeloFraenkelSO ZermeloFraenkelSO => _zermeloFraenkelSO;
    }
}

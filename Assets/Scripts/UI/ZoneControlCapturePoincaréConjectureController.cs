using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePoincaréConjectureController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePoincaréConjectureSO _poincaréSO;
        [SerializeField] private PlayerWallet                           _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPoincaréConjectureProved;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _ricciFlowLabel;
        [SerializeField] private Text       _proofLabel;
        [SerializeField] private Slider     _ricciFlowBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleProofDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleProofDelegate        = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPoincaréConjectureProved?.RegisterCallback(_handleProofDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPoincaréConjectureProved?.UnregisterCallback(_handleProofDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_poincaréSO == null) return;
            int bonus = _poincaréSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_poincaréSO == null) return;
            _poincaréSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _poincaréSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_poincaréSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_ricciFlowLabel != null)
                _ricciFlowLabel.text = $"Ricci Flow Steps: {_poincaréSO.RicciFlowSteps}/{_poincaréSO.RicciFlowStepsNeeded}";

            if (_proofLabel != null)
                _proofLabel.text = $"Proofs: {_poincaréSO.ProofCount}";

            if (_ricciFlowBar != null)
                _ricciFlowBar.value = _poincaréSO.RicciFlowStepProgress;
        }

        public ZoneControlCapturePoincaréConjectureSO PoincaréSO => _poincaréSO;
    }
}

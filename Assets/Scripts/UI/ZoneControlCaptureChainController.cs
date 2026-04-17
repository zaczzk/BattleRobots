using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    /// <summary>
    /// UI MonoBehaviour that bridges zone-capture events into
    /// <see cref="ZoneControlCaptureChainSO"/>, awards the chain bonus to the
    /// player's wallet on each completed chain, and displays chain progress.
    ///
    /// <c>_onPlayerZoneCaptured</c> (IntGameEvent): records a capture; on chain
    ///   completion awards wallet bonus + Refresh.
    /// <c>_onMatchStarted</c>: resets the SO + Refresh.
    /// <c>_onChainCompleted</c>: Refresh.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureChainController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureChainSO _chainSO;
        [SerializeField] private PlayerWallet              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private IntGameEvent  _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onChainCompleted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _chainLabel;
        [SerializeField] private Text       _totalLabel;
        [SerializeField] private GameObject _panel;

        private Action<int> _handlePlayerZoneCapturedDelegate;
        private Action      _handleMatchStartedDelegate;
        private Action      _refreshDelegate;

        private int _completedChainsPrev;

        private void Awake()
        {
            _handlePlayerZoneCapturedDelegate = HandlePlayerZoneCaptured;
            _handleMatchStartedDelegate       = HandleMatchStarted;
            _refreshDelegate                  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerZoneCapturedDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onChainCompleted?.RegisterCallback(_refreshDelegate);
            _completedChainsPrev = _chainSO?.CompletedChains ?? 0;
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerZoneCapturedDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onChainCompleted?.UnregisterCallback(_refreshDelegate);
        }

        private void HandlePlayerZoneCaptured(int zoneIndex)
        {
            if (_chainSO == null)
            {
                Refresh();
                return;
            }

            int prevChains = _chainSO.CompletedChains;
            _chainSO.AddCapture(zoneIndex);

            if (_chainSO.CompletedChains > prevChains && _wallet != null)
                _wallet.AddFunds(_chainSO.ChainBonus);

            Refresh();
        }

        private void HandleMatchStarted()
        {
            _chainSO?.Reset();
            _completedChainsPrev = 0;
            Refresh();
        }

        public void Refresh()
        {
            if (_chainSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_chainLabel != null)
                _chainLabel.text = $"Chain: {_chainSO.CurrentChainLength}/{_chainSO.ChainTarget}";

            if (_totalLabel != null)
                _totalLabel.text = $"Chains: {_chainSO.CompletedChains}";
        }

        public ZoneControlCaptureChainSO ChainSO => _chainSO;
    }
}

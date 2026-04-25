using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureTarskiUndefinabilityController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureTarskiUndefinabilitySO _tarskiUndefinabilitySO;
        [SerializeField] private PlayerWallet                              _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onTarskiUndefinabilityReached;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _truthPredicateLabel;
        [SerializeField] private Text       _undefinabilityCountLabel;
        [SerializeField] private Slider     _truthPredicateBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleReachedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleReachedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onTarskiUndefinabilityReached?.RegisterCallback(_handleReachedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onTarskiUndefinabilityReached?.UnregisterCallback(_handleReachedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_tarskiUndefinabilitySO == null) return;
            int bonus = _tarskiUndefinabilitySO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_tarskiUndefinabilitySO == null) return;
            _tarskiUndefinabilitySO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _tarskiUndefinabilitySO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_tarskiUndefinabilitySO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_truthPredicateLabel != null)
                _truthPredicateLabel.text =
                    $"Truth Predicates: {_tarskiUndefinabilitySO.TruthPredicates}/{_tarskiUndefinabilitySO.TruthPredicatesNeeded}";

            if (_undefinabilityCountLabel != null)
                _undefinabilityCountLabel.text = $"Undefinabilities: {_tarskiUndefinabilitySO.UndefinabilityCount}";

            if (_truthPredicateBar != null)
                _truthPredicateBar.value = _tarskiUndefinabilitySO.TruthPredicateProgress;
        }

        public ZoneControlCaptureTarskiUndefinabilitySO TarskiUndefinabilitySO => _tarskiUndefinabilitySO;
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureDerivedFunctorController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureDerivedFunctorSO _derivedFunctorSO;
        [SerializeField] private PlayerWallet                        _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onDerivedFunctorDerived;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _resolutionLabel;
        [SerializeField] private Text       _deriveLabel;
        [SerializeField] private Slider     _resolutionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleDerivedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleDerivedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onDerivedFunctorDerived?.RegisterCallback(_handleDerivedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onDerivedFunctorDerived?.UnregisterCallback(_handleDerivedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_derivedFunctorSO == null) return;
            int bonus = _derivedFunctorSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_derivedFunctorSO == null) return;
            _derivedFunctorSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _derivedFunctorSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_derivedFunctorSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_resolutionLabel != null)
                _resolutionLabel.text = $"Resolutions: {_derivedFunctorSO.Resolutions}/{_derivedFunctorSO.ResolutionsNeeded}";

            if (_deriveLabel != null)
                _deriveLabel.text = $"Derivations: {_derivedFunctorSO.DeriveCount}";

            if (_resolutionBar != null)
                _resolutionBar.value = _derivedFunctorSO.ResolutionProgress;
        }

        public ZoneControlCaptureDerivedFunctorSO DerivedFunctorSO => _derivedFunctorSO;
    }
}

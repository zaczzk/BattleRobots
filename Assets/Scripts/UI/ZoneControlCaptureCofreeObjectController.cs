using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCofreeObjectController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCofreeObjectSO _cofreeObjectSO;
        [SerializeField] private PlayerWallet                      _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCofreeObjectCofreed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _cofactorLabel;
        [SerializeField] private Text       _cofreeObjectLabel;
        [SerializeField] private Slider     _cofactorBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleCofreedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleCofreedDelegate      = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCofreeObjectCofreed?.RegisterCallback(_handleCofreedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCofreeObjectCofreed?.UnregisterCallback(_handleCofreedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cofreeObjectSO == null) return;
            int bonus = _cofreeObjectSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cofreeObjectSO == null) return;
            _cofreeObjectSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cofreeObjectSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cofreeObjectSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_cofactorLabel != null)
                _cofactorLabel.text = $"Cofactors: {_cofreeObjectSO.Cofactors}/{_cofreeObjectSO.CofactorsNeeded}";

            if (_cofreeObjectLabel != null)
                _cofreeObjectLabel.text = $"Cofree Objects: {_cofreeObjectSO.CofreeObjectCount}";

            if (_cofactorBar != null)
                _cofactorBar.value = _cofreeObjectSO.CofactorProgress;
        }

        public ZoneControlCaptureCofreeObjectSO CofreeObjectSO => _cofreeObjectSO;
    }
}

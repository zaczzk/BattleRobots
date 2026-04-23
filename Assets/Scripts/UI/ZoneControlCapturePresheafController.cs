using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCapturePresheafController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCapturePresheafSO _presheafSO;
        [SerializeField] private PlayerWallet                  _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onPresheafFormed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _restrictionLabel;
        [SerializeField] private Text       _presheafLabel;
        [SerializeField] private Slider     _restrictionBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFormedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFormedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onPresheafFormed?.RegisterCallback(_handleFormedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onPresheafFormed?.UnregisterCallback(_handleFormedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_presheafSO == null) return;
            int bonus = _presheafSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_presheafSO == null) return;
            _presheafSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _presheafSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_presheafSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_restrictionLabel != null)
                _restrictionLabel.text = $"Restrictions: {_presheafSO.Restrictions}/{_presheafSO.RestrictionsNeeded}";

            if (_presheafLabel != null)
                _presheafLabel.text = $"Presheaves: {_presheafSO.PresheafCount}";

            if (_restrictionBar != null)
                _restrictionBar.value = _presheafSO.RestrictionProgress;
        }

        public ZoneControlCapturePresheafSO PresheafSO => _presheafSO;
    }
}

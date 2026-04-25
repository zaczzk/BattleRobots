using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureInfinityGroupoidController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureInfinityGroupoidSO _infinityGroupoidSO;
        [SerializeField] private PlayerWallet                          _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onInfinityGroupoidFilled;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _hornFillingLabel;
        [SerializeField] private Text       _fillLabel;
        [SerializeField] private Slider     _hornFillingBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFilledDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleFilledDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onInfinityGroupoidFilled?.RegisterCallback(_handleFilledDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onInfinityGroupoidFilled?.UnregisterCallback(_handleFilledDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_infinityGroupoidSO == null) return;
            int bonus = _infinityGroupoidSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_infinityGroupoidSO == null) return;
            _infinityGroupoidSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _infinityGroupoidSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_infinityGroupoidSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_hornFillingLabel != null)
                _hornFillingLabel.text = $"Horn Fillings: {_infinityGroupoidSO.HornFillings}/{_infinityGroupoidSO.HornFillingsNeeded}";

            if (_fillLabel != null)
                _fillLabel.text = $"Fills: {_infinityGroupoidSO.FillCount}";

            if (_hornFillingBar != null)
                _hornFillingBar.value = _infinityGroupoidSO.HornFillingProgress;
        }

        public ZoneControlCaptureInfinityGroupoidSO InfinityGroupoidSO => _infinityGroupoidSO;
    }
}

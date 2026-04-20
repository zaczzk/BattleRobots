using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureFurnaceController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureFurnaceSO _furnaceSO;
        [SerializeField] private PlayerWallet                _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onFurnaceSmelted;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _fuelLabel;
        [SerializeField] private Text       _smeltLabel;
        [SerializeField] private Slider     _fuelBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleFurnaceSmeltedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate          = HandlePlayerCaptured;
            _handleBotDelegate             = HandleBotCaptured;
            _handleMatchStartedDelegate    = HandleMatchStarted;
            _handleFurnaceSmeltedDelegate  = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onFurnaceSmelted?.RegisterCallback(_handleFurnaceSmeltedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onFurnaceSmelted?.UnregisterCallback(_handleFurnaceSmeltedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_furnaceSO == null) return;
            int bonus = _furnaceSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_furnaceSO == null) return;
            _furnaceSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _furnaceSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_furnaceSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_fuelLabel != null)
                _fuelLabel.text = $"Fuel: {_furnaceSO.Fuel}/{_furnaceSO.FuelNeeded}";

            if (_smeltLabel != null)
                _smeltLabel.text = $"Smelts: {_furnaceSO.SmeltCount}";

            if (_fuelBar != null)
                _fuelBar.value = _furnaceSO.FuelProgress;
        }

        public ZoneControlCaptureFurnaceSO FurnaceSO => _furnaceSO;
    }
}

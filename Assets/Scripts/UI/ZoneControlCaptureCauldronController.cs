using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCauldronController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCauldronSO _cauldronSO;
        [SerializeField] private PlayerWallet                 _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCauldronBrewed;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _ingredientLabel;
        [SerializeField] private Text       _brewLabel;
        [SerializeField] private Slider     _ingredientBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleBrewedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleBrewedDelegate       = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCauldronBrewed?.RegisterCallback(_handleBrewedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCauldronBrewed?.UnregisterCallback(_handleBrewedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cauldronSO == null) return;
            int bonus = _cauldronSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cauldronSO == null) return;
            _cauldronSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cauldronSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cauldronSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_ingredientLabel != null)
                _ingredientLabel.text = $"Ingredients: {_cauldronSO.Ingredients}/{_cauldronSO.IngredientsNeeded}";

            if (_brewLabel != null)
                _brewLabel.text = $"Brews: {_cauldronSO.BrewCount}";

            if (_ingredientBar != null)
                _ingredientBar.value = _cauldronSO.IngredientProgress;
        }

        public ZoneControlCaptureCauldronSO CauldronSO => _cauldronSO;
    }
}

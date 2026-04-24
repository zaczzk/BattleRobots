using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureCobordismRingController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureCobordismRingSO _cobordismRingSO;
        [SerializeField] private PlayerWallet                       _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onCobordismRingMultiplied;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _generatorLabel;
        [SerializeField] private Text       _multiplyLabel;
        [SerializeField] private Slider     _generatorBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleMultipliedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate       = HandlePlayerCaptured;
            _handleBotDelegate          = HandleBotCaptured;
            _handleMatchStartedDelegate = HandleMatchStarted;
            _handleMultipliedDelegate   = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onCobordismRingMultiplied?.RegisterCallback(_handleMultipliedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onCobordismRingMultiplied?.UnregisterCallback(_handleMultipliedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_cobordismRingSO == null) return;
            int bonus = _cobordismRingSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_cobordismRingSO == null) return;
            _cobordismRingSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _cobordismRingSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_cobordismRingSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_generatorLabel != null)
                _generatorLabel.text = $"Generators: {_cobordismRingSO.Generators}/{_cobordismRingSO.GeneratorsNeeded}";

            if (_multiplyLabel != null)
                _multiplyLabel.text = $"Multiplications: {_cobordismRingSO.MultiplicationCount}";

            if (_generatorBar != null)
                _generatorBar.value = _cobordismRingSO.GeneratorProgress;
        }

        public ZoneControlCaptureCobordismRingSO CobordismRingSO => _cobordismRingSO;
    }
}

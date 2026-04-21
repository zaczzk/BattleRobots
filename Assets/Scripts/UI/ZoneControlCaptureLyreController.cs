using System;
using UnityEngine;
using UnityEngine.UI;
using BattleRobots.Core;

namespace BattleRobots.UI
{
    [DisallowMultipleComponent]
    public sealed class ZoneControlCaptureLyreController : MonoBehaviour
    {
        [Header("Data (optional)")]
        [SerializeField] private ZoneControlCaptureLyreSO _lyreSO;
        [SerializeField] private PlayerWallet             _wallet;

        [Header("Event Channels — In (optional)")]
        [SerializeField] private VoidGameEvent _onPlayerZoneCaptured;
        [SerializeField] private VoidGameEvent _onBotZoneCaptured;
        [SerializeField] private VoidGameEvent _onMatchStarted;
        [SerializeField] private VoidGameEvent _onLyreHarmonized;

        [Header("UI References (optional)")]
        [SerializeField] private Text       _noteLabel;
        [SerializeField] private Text       _harmonyLabel;
        [SerializeField] private Slider     _noteBar;
        [SerializeField] private GameObject _panel;

        private Action _handlePlayerDelegate;
        private Action _handleBotDelegate;
        private Action _handleMatchStartedDelegate;
        private Action _handleLyreHarmonizedDelegate;

        private void Awake()
        {
            _handlePlayerDelegate         = HandlePlayerCaptured;
            _handleBotDelegate            = HandleBotCaptured;
            _handleMatchStartedDelegate   = HandleMatchStarted;
            _handleLyreHarmonizedDelegate = Refresh;
        }

        private void OnEnable()
        {
            _onPlayerZoneCaptured?.RegisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.RegisterCallback(_handleBotDelegate);
            _onMatchStarted?.RegisterCallback(_handleMatchStartedDelegate);
            _onLyreHarmonized?.RegisterCallback(_handleLyreHarmonizedDelegate);
            Refresh();
        }

        private void OnDisable()
        {
            _onPlayerZoneCaptured?.UnregisterCallback(_handlePlayerDelegate);
            _onBotZoneCaptured?.UnregisterCallback(_handleBotDelegate);
            _onMatchStarted?.UnregisterCallback(_handleMatchStartedDelegate);
            _onLyreHarmonized?.UnregisterCallback(_handleLyreHarmonizedDelegate);
        }

        private void HandlePlayerCaptured()
        {
            if (_lyreSO == null) return;
            int bonus = _lyreSO.RecordPlayerCapture();
            if (bonus > 0) _wallet?.AddFunds(bonus);
            Refresh();
        }

        private void HandleBotCaptured()
        {
            if (_lyreSO == null) return;
            _lyreSO.RecordBotCapture();
            Refresh();
        }

        private void HandleMatchStarted()
        {
            _lyreSO?.Reset();
            Refresh();
        }

        public void Refresh()
        {
            if (_lyreSO == null)
            {
                _panel?.SetActive(false);
                return;
            }

            _panel?.SetActive(true);

            if (_noteLabel != null)
                _noteLabel.text = $"Notes: {_lyreSO.Notes}/{_lyreSO.NotesNeeded}";

            if (_harmonyLabel != null)
                _harmonyLabel.text = $"Harmonies: {_lyreSO.HarmonyCount}";

            if (_noteBar != null)
                _noteBar.value = _lyreSO.NoteProgress;
        }

        public ZoneControlCaptureLyreSO LyreSO => _lyreSO;
    }
}

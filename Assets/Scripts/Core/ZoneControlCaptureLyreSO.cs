using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureLyre", order = 277)]
    public sealed class ZoneControlCaptureLyreSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _notesNeeded    = 6;
        [SerializeField, Min(1)] private int _discordPerBot  = 2;
        [SerializeField, Min(0)] private int _bonusPerHarmony = 895;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onLyreHarmonized;

        private int _notes;
        private int _harmonyCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   NotesNeeded       => _notesNeeded;
        public int   DiscordPerBot     => _discordPerBot;
        public int   BonusPerHarmony   => _bonusPerHarmony;
        public int   Notes             => _notes;
        public int   HarmonyCount      => _harmonyCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float NoteProgress      => _notesNeeded > 0
            ? Mathf.Clamp01(_notes / (float)_notesNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _notes = Mathf.Min(_notes + 1, _notesNeeded);
            if (_notes >= _notesNeeded)
            {
                int bonus = _bonusPerHarmony;
                _harmonyCount++;
                _totalBonusAwarded += bonus;
                _notes              = 0;
                _onLyreHarmonized?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _notes = Mathf.Max(0, _notes - _discordPerBot);
        }

        public void Reset()
        {
            _notes             = 0;
            _harmonyCount      = 0;
            _totalBonusAwarded = 0;
        }
    }
}

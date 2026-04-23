using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureYoneda", order = 379)]
    public sealed class ZoneControlCaptureYonedaSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _embedsNeeded   = 5;
        [SerializeField, Min(1)] private int _dissolvePerBot = 1;
        [SerializeField, Min(0)] private int _bonusPerEmbed  = 2425;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onYonedaEmbedded;

        private int _embeds;
        private int _embedCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   EmbedsNeeded      => _embedsNeeded;
        public int   DissolvePerBot    => _dissolvePerBot;
        public int   BonusPerEmbed     => _bonusPerEmbed;
        public int   Embeds            => _embeds;
        public int   EmbedCount        => _embedCount;
        public int   TotalBonusAwarded => _totalBonusAwarded;
        public float EmbedProgress     => _embedsNeeded > 0
            ? Mathf.Clamp01(_embeds / (float)_embedsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _embeds = Mathf.Min(_embeds + 1, _embedsNeeded);
            if (_embeds >= _embedsNeeded)
            {
                int bonus = _bonusPerEmbed;
                _embedCount++;
                _totalBonusAwarded += bonus;
                _embeds             = 0;
                _onYonedaEmbedded?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _embeds = Mathf.Max(0, _embeds - _dissolvePerBot);
        }

        public void Reset()
        {
            _embeds            = 0;
            _embedCount        = 0;
            _totalBonusAwarded = 0;
        }
    }
}

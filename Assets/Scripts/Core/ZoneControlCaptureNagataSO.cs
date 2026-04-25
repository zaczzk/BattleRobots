using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureNagata", order = 516)]
    public sealed class ZoneControlCaptureNagataSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _embeddingsNeeded          = 5;
        [SerializeField, Min(1)] private int _degenerationsPerBot       = 1;
        [SerializeField, Min(0)] private int _bonusPerCompactification  = 4480;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onNagataCompactified;

        private int _embeddings;
        private int _compactificationCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   EmbeddingsNeeded         => _embeddingsNeeded;
        public int   DegenerationsPerBot      => _degenerationsPerBot;
        public int   BonusPerCompactification => _bonusPerCompactification;
        public int   Embeddings               => _embeddings;
        public int   CompactificationCount    => _compactificationCount;
        public int   TotalBonusAwarded        => _totalBonusAwarded;
        public float EmbeddingProgress        => _embeddingsNeeded > 0
            ? Mathf.Clamp01(_embeddings / (float)_embeddingsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _embeddings = Mathf.Min(_embeddings + 1, _embeddingsNeeded);
            if (_embeddings >= _embeddingsNeeded)
            {
                int bonus = _bonusPerCompactification;
                _compactificationCount++;
                _totalBonusAwarded += bonus;
                _embeddings         = 0;
                _onNagataCompactified?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _embeddings = Mathf.Max(0, _embeddings - _degenerationsPerBot);
        }

        public void Reset()
        {
            _embeddings            = 0;
            _compactificationCount = 0;
            _totalBonusAwarded     = 0;
        }
    }
}

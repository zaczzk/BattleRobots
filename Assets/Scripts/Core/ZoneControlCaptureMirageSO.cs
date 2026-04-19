using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureMirage", order = 198)]
    public sealed class ZoneControlCaptureMirageSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(0)] private int _stackBonus  = 25;
        [SerializeField, Min(1)] private int _maxStacks   = 8;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onMirageStack;

        private int _currentStacks;
        private int _totalMirageBonus;
        private int _mirageCaptures;

        private void OnEnable() => Reset();

        public int   StackBonus       => _stackBonus;
        public int   MaxStacks        => _maxStacks;
        public int   CurrentStacks    => _currentStacks;
        public int   TotalMirageBonus => _totalMirageBonus;
        public int   MirageCaptures   => _mirageCaptures;
        public float StackProgress    => _maxStacks > 0
            ? Mathf.Clamp01(_currentStacks / (float)_maxStacks)
            : 0f;

        public int RecordPlayerCapture()
        {
            int newStack = Mathf.Min(_currentStacks + 1, _maxStacks);
            if (newStack != _currentStacks)
                _onMirageStack?.Raise();
            _currentStacks = newStack;
            int bonus = _stackBonus * _currentStacks;
            _totalMirageBonus += bonus;
            _mirageCaptures++;
            return bonus;
        }

        public void RecordBotCapture()
        {
            _currentStacks = 0;
        }

        public void Reset()
        {
            _currentStacks    = 0;
            _totalMirageBonus = 0;
            _mirageCaptures   = 0;
        }
    }
}

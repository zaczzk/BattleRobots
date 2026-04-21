using UnityEngine;

namespace BattleRobots.Core
{
    [CreateAssetMenu(menuName = "BattleRobots/Arena/ZoneControlCaptureCauldron", order = 279)]
    public sealed class ZoneControlCaptureCauldronSO : ScriptableObject
    {
        [Header("Config")]
        [SerializeField, Min(1)] private int _ingredientsNeeded = 5;
        [SerializeField, Min(1)] private int _dilutePerBot      = 1;
        [SerializeField, Min(0)] private int _bonusPerBrew      = 925;

        [Header("Event Channels (optional)")]
        [SerializeField] private VoidGameEvent _onCauldronBrewed;

        private int _ingredients;
        private int _brewCount;
        private int _totalBonusAwarded;

        private void OnEnable() => Reset();

        public int   IngredientsNeeded  => _ingredientsNeeded;
        public int   DilutePerBot       => _dilutePerBot;
        public int   BonusPerBrew       => _bonusPerBrew;
        public int   Ingredients        => _ingredients;
        public int   BrewCount          => _brewCount;
        public int   TotalBonusAwarded  => _totalBonusAwarded;
        public float IngredientProgress => _ingredientsNeeded > 0
            ? Mathf.Clamp01(_ingredients / (float)_ingredientsNeeded)
            : 0f;

        public int RecordPlayerCapture()
        {
            _ingredients = Mathf.Min(_ingredients + 1, _ingredientsNeeded);
            if (_ingredients >= _ingredientsNeeded)
            {
                int bonus = _bonusPerBrew;
                _brewCount++;
                _totalBonusAwarded += bonus;
                _ingredients        = 0;
                _onCauldronBrewed?.Raise();
                return bonus;
            }
            return 0;
        }

        public void RecordBotCapture()
        {
            _ingredients = Mathf.Max(0, _ingredients - _dilutePerBot);
        }

        public void Reset()
        {
            _ingredients       = 0;
            _brewCount         = 0;
            _totalBonusAwarded = 0;
        }
    }
}

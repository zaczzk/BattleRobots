using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Typed SO event channel. Raise() broadcasts to all registered listeners.
    /// Assets are immutable at runtime — no fields mutated; listener list is transient.
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Events/GameEvent<T>", order = 0)]
    public abstract class GameEvent<T> : ScriptableObject
    {
        // Transient listener list — not serialized, not an allocation in Update.
        private readonly List<GameEventListener<T>> _listeners = new List<GameEventListener<T>>();

        public void Raise(T value)
        {
            // Iterate in reverse so listeners can safely unregister during callback.
            for (int i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i].OnEventRaised(value);
            }
        }

        public void RegisterListener(GameEventListener<T> listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void UnregisterListener(GameEventListener<T> listener)
        {
            _listeners.Remove(listener);
        }

#if UNITY_EDITOR
        [ContextMenu("Raise (Editor Only — dummy default)")]
        private void RaiseEditorDefault() => Raise(default);
#endif
    }
}

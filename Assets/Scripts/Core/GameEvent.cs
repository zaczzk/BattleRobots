using System;
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

        // Direct Action callbacks — allows non-MonoBehaviour subscribers (e.g. VFX handlers)
        // to subscribe without requiring a separate listener component wired in the Inspector.
        private readonly List<Action<T>> _callbacks = new List<Action<T>>();

        public void Raise(T value)
        {
            // Iterate in reverse so listeners/callbacks can safely unregister during invocation.
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised(value);

            for (int i = _callbacks.Count - 1; i >= 0; i--)
                _callbacks[i]?.Invoke(value);
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

        /// <summary>
        /// Register a direct callback. Useful for components that should not require
        /// a separate <see cref="GameEventListener{T}"/> component wired in the Inspector.
        /// Guard against duplicate registration is applied.
        /// </summary>
        public void RegisterCallback(Action<T> callback)
        {
            if (!_callbacks.Contains(callback))
                _callbacks.Add(callback);
        }

        /// <summary>Unregister a previously registered direct callback.</summary>
        public void UnregisterCallback(Action<T> callback)
        {
            _callbacks.Remove(callback);
        }

#if UNITY_EDITOR
        [ContextMenu("Raise (Editor Only — dummy default)")]
        private void RaiseEditorDefault() => Raise(default);
#endif
    }
}

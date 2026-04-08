using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleRobots.Core
{
    /// <summary>
    /// Parameter-less SO event channel for signals that carry no data
    /// (e.g., MatchStarted, RobotDestroyed).
    /// </summary>
    [CreateAssetMenu(menuName = "BattleRobots/Events/VoidGameEvent", order = 1)]
    public sealed class VoidGameEvent : ScriptableObject
    {
        private readonly List<VoidGameEventListener> _listeners = new List<VoidGameEventListener>();

        // Direct Action callbacks — allows non-MonoBehaviour subscribers (e.g. VFX handlers)
        // to subscribe without requiring a separate listener component wired in the Inspector.
        private readonly List<Action> _callbacks = new List<Action>();

        public void Raise()
        {
            // Iterate in reverse so listeners/callbacks can safely unregister during invocation.
            for (int i = _listeners.Count - 1; i >= 0; i--)
                _listeners[i].OnEventRaised();

            for (int i = _callbacks.Count - 1; i >= 0; i--)
                _callbacks[i]?.Invoke();
        }

        public void RegisterListener(VoidGameEventListener listener)
        {
            if (!_listeners.Contains(listener))
                _listeners.Add(listener);
        }

        public void UnregisterListener(VoidGameEventListener listener)
        {
            _listeners.Remove(listener);
        }

        /// <summary>
        /// Register a direct callback. Useful for components that should not require
        /// a separate <see cref="VoidGameEventListener"/> component wired in the Inspector.
        /// Guard against duplicate registration is applied.
        /// </summary>
        public void RegisterCallback(Action callback)
        {
            if (!_callbacks.Contains(callback))
                _callbacks.Add(callback);
        }

        /// <summary>Unregister a previously registered direct callback.</summary>
        public void UnregisterCallback(Action callback)
        {
            _callbacks.Remove(callback);
        }

#if UNITY_EDITOR
        [ContextMenu("Raise (Editor Only)")]
        private void RaiseEditor() => Raise();
#endif
    }
}

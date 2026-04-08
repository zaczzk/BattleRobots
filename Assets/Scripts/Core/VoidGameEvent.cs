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

        // Delegate callbacks for code-side subscribers (e.g. VFX handlers) — avoids UnityEvent bridge.
        private readonly List<Action> _callbacks = new List<Action>();

        public void Raise()
        {
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

        /// <summary>Register an Action callback. Cache the delegate in Awake; call in OnEnable.</summary>
        public void RegisterCallback(Action callback)
        {
            if (!_callbacks.Contains(callback))
                _callbacks.Add(callback);
        }

        /// <summary>Unregister an Action callback. Call in OnDisable.</summary>
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

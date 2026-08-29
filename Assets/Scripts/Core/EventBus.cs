using System;
using System.Collections.Generic;
using UnityEngine;

namespace MetaCricket.Core
{
    /// <summary>
    /// Generic type-safe event bus implementing pub/sub pattern.
    /// Allows decoupled communication between game systems.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _eventHandlers = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Subscribe to an event of type T.
        /// </summary>
        /// <typeparam name="T">The event type to subscribe to.</typeparam>
        /// <param name="handler">The callback to invoke when the event is published.</param>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Type eventType = typeof(T);

            if (_eventHandlers.TryGetValue(eventType, out Delegate existingHandler))
            {
                _eventHandlers[eventType] = Delegate.Combine(existingHandler, handler);
            }
            else
            {
                _eventHandlers[eventType] = handler;
            }
        }

        /// <summary>
        /// Unsubscribe from an event of type T.
        /// </summary>
        /// <typeparam name="T">The event type to unsubscribe from.</typeparam>
        /// <param name="handler">The callback to remove.</param>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            Type eventType = typeof(T);

            if (_eventHandlers.TryGetValue(eventType, out Delegate existingHandler))
            {
                Delegate updatedHandler = Delegate.Remove(existingHandler, handler);

                if (updatedHandler == null)
                {
                    _eventHandlers.Remove(eventType);
                }
                else
                {
                    _eventHandlers[eventType] = updatedHandler;
                }
            }
        }

        /// <summary>
        /// Publish an event of type T to all subscribers.
        /// </summary>
        /// <typeparam name="T">The event type to publish.</typeparam>
        /// <param name="eventData">The event data to send.</param>
        public static void Publish<T>(T eventData) where T : struct
        {
            Type eventType = typeof(T);

            if (_eventHandlers.TryGetValue(eventType, out Delegate existingHandler))
            {
                (existingHandler as Action<T>)?.Invoke(eventData);
            }
        }

        /// <summary>
        /// Check if there are any subscribers for an event type.
        /// </summary>
        /// <typeparam name="T">The event type to check.</typeparam>
        /// <returns>True if there are subscribers.</returns>
        public static bool HasSubscribers<T>() where T : struct
        {
            return _eventHandlers.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Clear all event subscriptions. Use with caution, typically on scene unload.
        /// </summary>
        public static void ClearAll()
        {
            _eventHandlers.Clear();
        }

        /// <summary>
        /// Clear all subscriptions for a specific event type.
        /// </summary>
        /// <typeparam name="T">The event type to clear subscriptions for.</typeparam>
        public static void Clear<T>() where T : struct
        {
            _eventHandlers.Remove(typeof(T));
        }

        /// <summary>
        /// Remove subscriptions whose delegate target is a destroyed Unity Object.
        /// Persistent singletons (DontDestroyOnLoad) retain their subscriptions,
        /// while destroyed scene-bound MonoBehaviours are cleaned up.
        /// </summary>
        public static void CleanupDestroyedSubscribers()
        {
            List<Type> keysToRemove = new List<Type>();

            List<Type> keys = new List<Type>(_eventHandlers.Keys);
            foreach (Type eventType in keys)
            {
                Delegate handler = _eventHandlers[eventType];
                if (handler == null)
                {
                    keysToRemove.Add(eventType);
                    continue;
                }

                Delegate[] invocationList = handler.GetInvocationList();
                Delegate cleanedHandler = null;

                foreach (Delegate del in invocationList)
                {
                    // If the delegate target is a Unity Object, check if it has been destroyed
                    if (del.Target is UnityEngine.Object unityObj)
                    {
                        // Unity overloads == to return true for null when object is destroyed
                        if (unityObj == null)
                        {
                            // Target is destroyed, skip this delegate
                            continue;
                        }
                    }

                    // Keep this delegate (target is alive or is not a Unity Object)
                    cleanedHandler = cleanedHandler == null ? del : Delegate.Combine(cleanedHandler, del);
                }

                if (cleanedHandler == null)
                {
                    keysToRemove.Add(eventType);
                }
                else
                {
                    _eventHandlers[eventType] = cleanedHandler;
                }
            }

            foreach (Type key in keysToRemove)
            {
                _eventHandlers.Remove(key);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MetaCricket.Core
{
    /// <summary>
    /// Static service locator providing decoupled dependency access.
    /// Services are registered by interface or base type and retrieved without direct references.
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        /// <summary>
        /// Register a service instance for a given type.
        /// </summary>
        /// <typeparam name="T">The type to register the service as (typically an interface).</typeparam>
        /// <param name="service">The service instance to register.</param>
        public static void Register<T>(T service) where T : class
        {
            Type type = typeof(T);

            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service of type {type.Name} is already registered. Overwriting.");
                _services[type] = service;
            }
            else
            {
                _services.Add(type, service);
            }
        }

        /// <summary>
        /// Get a registered service instance by type.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>The registered service instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the requested service is not registered.</exception>
        public static T Get<T>() where T : class
        {
            Type type = typeof(T);

            if (_services.TryGetValue(type, out object service))
            {
                return (T)service;
            }

            throw new InvalidOperationException(
                $"[ServiceLocator] Service of type {type.Name} is not registered. " +
                "Make sure it is registered before attempting to retrieve it.");
        }

        /// <summary>
        /// Try to get a registered service instance by type.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <param name="service">The registered service instance if found.</param>
        /// <returns>True if the service was found, false otherwise.</returns>
        public static bool TryGet<T>(out T service) where T : class
        {
            Type type = typeof(T);

            if (_services.TryGetValue(type, out object existingService))
            {
                service = (T)existingService;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Unregister a service by type.
        /// </summary>
        /// <typeparam name="T">The type of service to unregister.</typeparam>
        public static void Unregister<T>() where T : class
        {
            Type type = typeof(T);

            if (_services.ContainsKey(type))
            {
                _services.Remove(type);
            }
            else
            {
                Debug.LogWarning($"[ServiceLocator] Attempted to unregister service of type {type.Name}, but it was not registered.");
            }
        }

        /// <summary>
        /// Check if a service of the given type is registered.
        /// </summary>
        /// <typeparam name="T">The type to check.</typeparam>
        /// <returns>True if the service is registered.</returns>
        public static bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Clear all registered services. Use with caution, typically during app shutdown.
        /// </summary>
        public static void ClearAll()
        {
            _services.Clear();
        }

        /// <summary>
        /// Remove services whose registered instance is a destroyed Unity Object.
        /// Persistent singletons (DontDestroyOnLoad) retain their registrations,
        /// while destroyed scene-bound MonoBehaviours are cleaned up.
        /// </summary>
        public static void CleanupDestroyedServices()
        {
            List<Type> keysToRemove = new List<Type>();

            foreach (var kvp in _services)
            {
                object service = kvp.Value;

                // If the service is a Unity Object, check if it has been destroyed
                if (service is UnityEngine.Object unityObj)
                {
                    // Unity overloads == to return true for null when object is destroyed
                    if (unityObj == null)
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
            }

            foreach (Type key in keysToRemove)
            {
                _services.Remove(key);
                Debug.Log($"[ServiceLocator] Cleaned up destroyed service: {key.Name}");
            }
        }
    }
}

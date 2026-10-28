using System;
using System.Collections.Generic;
using UnityEngine;

namespace PatientZero.Core
{
    /// <summary>
    /// One registered implementation per contract interface (see Interfaces.cs).
    /// Implementations register in OnEnable and unregister in OnDisable.
    ///
    /// A destroyed Unity object that forgot to unregister is treated as missing rather
    /// than returned, so callers never touch a dead component.
    /// No FindObjectOfType, no singletons with static Instance fields elsewhere.
    /// </summary>
    public static class Services<T> where T : class
    {
        private static T _instance;

        static Services()
        {
            ServiceResets.Add(Reset);
        }

        public static T Instance
        {
            get
            {
                T service;
                TryGet(out service);
                return service;
            }
        }

        public static bool TryGet(out T service)
        {
            T current = _instance;
            if (current != null)
            {
                UnityEngine.Object unityObject = current as UnityEngine.Object;
                if (!ReferenceEquals(unityObject, null) && unityObject == null)
                {
                    _instance = null; // destroyed without unregistering
                    current = null;
                }
            }
            service = current;
            return current != null;
        }

        /// <summary>
        /// Registers the implementation. A second live implementation is refused and
        /// logged as an error: two rigs or two directors is a scene bug, not a handover.
        /// </summary>
        public static bool Register(T service)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            T existing;
            if (TryGet(out existing) && !ReferenceEquals(existing, service))
            {
                Debug.LogError("[PatientZero] Second " + typeof(T).Name + " refused; one is already registered.");
                return false;
            }
            _instance = service;
            return true;
        }

        public static void Unregister(T service)
        {
            if (ReferenceEquals(_instance, service)) _instance = null;
        }

        private static void Reset()
        {
            _instance = null;
        }
    }

    internal static class ServiceResets
    {
        private static readonly List<Action> Resets = new List<Action>();

        internal static void Add(Action reset)
        {
            Resets.Add(reset);
        }

        internal static void ResetAll()
        {
            for (int i = 0; i < Resets.Count; i++) Resets[i]();
        }
    }

    /// <summary>Clears static state before the first scene loads, so play mode starts clean
    /// even with domain reload disabled.</summary>
    public static class CoreStaticReset
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetStatics()
        {
            GameEvents.ResetAll();
            ServiceResets.ResetAll();
            Cues.ResetWarning();
        }
    }
}

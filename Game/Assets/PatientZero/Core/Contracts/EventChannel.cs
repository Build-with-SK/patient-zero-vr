using System;
using System.Collections.Generic;
using UnityEngine;

namespace PatientZero.Core
{
    /// <summary>
    /// A typed event with one payload struct.
    ///
    /// Raise() never allocates. A handler that throws is logged and the rest still run.
    /// Handlers may subscribe or unsubscribe while the event is being raised: a removed
    /// handler is skipped at once, an added one first runs on the next Raise().
    ///
    /// Rule for every subscriber: subscribe in OnEnable, unsubscribe in OnDisable.
    /// Channels are static and outlive scene reloads, so a missed unsubscribe leaks.
    /// </summary>
    public sealed class EventChannel<T> where T : struct
    {
        private static readonly Predicate<Action<T>> IsNullHandler = h => h == null;

        private readonly List<Action<T>> _handlers = new List<Action<T>>(8);
        private int _raiseDepth;
        private bool _needsCompact;

        public EventChannel(string name)
        {
            Name = name;
        }

        public string Name { get; }

        /// <summary>Live subscribers. Tests use it to catch leaked subscriptions after a restart.</summary>
        public int SubscriberCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _handlers.Count; i++)
                {
                    if (_handlers[i] != null) count++;
                }
                return count;
            }
        }

        public void Subscribe(Action<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            for (int i = 0; i < _handlers.Count; i++)
            {
                if (_handlers[i] == handler) return; // already subscribed
            }
            _handlers.Add(handler);
        }

        public void Unsubscribe(Action<T> handler)
        {
            if (handler == null) return;
            for (int i = 0; i < _handlers.Count; i++)
            {
                if (_handlers[i] != handler) continue;
                if (_raiseDepth > 0)
                {
                    _handlers[i] = null;
                    _needsCompact = true;
                }
                else
                {
                    _handlers.RemoveAt(i);
                }
                return;
            }
        }

        public void Raise(T payload)
        {
            int count = _handlers.Count;
            _raiseDepth++;
            try
            {
                for (int i = 0; i < count && i < _handlers.Count; i++)
                {
                    Action<T> handler = _handlers[i];
                    if (handler == null) continue;
                    try
                    {
                        handler(payload);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                _raiseDepth--;
                if (_raiseDepth == 0 && _needsCompact)
                {
                    _handlers.RemoveAll(IsNullHandler);
                    _needsCompact = false;
                }
            }
        }

        /// <summary>Drops every subscriber. Safe to call from inside a handler.</summary>
        public void Clear()
        {
            if (_raiseDepth > 0)
            {
                for (int i = 0; i < _handlers.Count; i++) _handlers[i] = null;
                _needsCompact = true;
            }
            else
            {
                _handlers.Clear();
            }
        }
    }
}

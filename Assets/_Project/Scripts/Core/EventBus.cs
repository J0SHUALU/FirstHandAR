using System;
using System.Collections.Generic;

namespace FirstHand.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            handlers.TryGetValue(type, out var existing);
            handlers[type] = Delegate.Combine(existing, handler);
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!handlers.TryGetValue(type, out var existing)) return;
            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null) handlers.Remove(type);
            else handlers[type] = remaining;
        }

        public static void Publish<T>(T evt) where T : struct
        {
            if (handlers.TryGetValue(typeof(T), out var d))
                (d as Action<T>)?.Invoke(evt);
        }

        public static void Clear() => handlers.Clear();
    }
}

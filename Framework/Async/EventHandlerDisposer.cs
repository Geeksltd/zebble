namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Olive;

    /// <summary>
    /// Provides a mechanism to prevent event handler dependency memory leaks.
    /// </summary>
    public class EventHandlerDisposer
    {
        readonly Dictionary<IAsyncEvent, WeakReference<IAsyncEventHandler>> Dependencies
             = new Dictionary<IAsyncEvent, WeakReference<IAsyncEventHandler>>();

        public IAsyncEvent[] Events
        {
            get
            {
                lock (Dependencies)
                    return Dependencies.Keys.ToArray();
            }
        }

        /// <summary>
        /// Will dispose all registered event handlers and clear them from the list.
        /// </summary>
        public void DisposeAll()
        {
            KeyValuePair<IAsyncEvent, WeakReference<IAsyncEventHandler>>[] list;
            lock (Dependencies)
                list = Dependencies.ToArray();

            foreach (var c in list)
            {
                c.Value.GetTargetOrDefault()?.RemoveSelf();
                c.Value.SetTarget(null);
            }

            lock (Dependencies) Dependencies.Clear();
        }

        public void RegisterUnique(IAsyncEvent @event, Func<IAsyncEventHandler> handlerCreator)
        {
            lock (Dependencies)
            {
                if (Dependencies.TryGetValue(@event, out var existing))
                {
                    var handler = existing.GetTargetOrDefault();
                    if (handler?.IsDisposed() == false) return;
                }

                Dependencies[@event] = handlerCreator().GetWeakReference();
            }
        }

        public void Register(IAsyncEvent @event, IAsyncEventHandler handler)
        {
            if (handler != null)
                lock (Dependencies)
                    Dependencies[@event] = handler.GetWeakReference();
        }
    }
}
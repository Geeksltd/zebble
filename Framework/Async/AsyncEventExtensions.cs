using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Olive;

namespace Zebble
{
    public static class AsyncEventExtensions
    {
        /// <summary>
        /// The same as RemoveHandler.
        /// It's added to get past the strange bug in C# for selecting the correct overload of RemoveHandler().
        /// </summary>
        public static TEvent RemoveActionHandler<TEvent>(this TEvent @event, Action handler)
            where TEvent : AbstractAsyncEvent
        {
            return @event.DoRemoveHandler(handler);
        }

        [DebuggerStepThrough]
        public static TEvent RemoveActionHandler<TEvent, TArg>(this TEvent @event, Action<TArg> handler)
            where TEvent : AbstractAsyncEvent
        {
            return @event.DoRemoveHandler(handler);
        }

        [DebuggerStepThrough]
        public static TEvent RemoveActionHandler<TEvent, TArg1, TArg2>(this TEvent @event, Action<TArg1, TArg2> handler)
            where TEvent : AbstractAsyncEvent
        {
            return @event.DoRemoveHandler(handler);
        }

        [DebuggerStepThrough]
        public static TEvent RemoveHandler<TEvent>(this TEvent @event, Func<Task> handler)
            where TEvent : AbstractAsyncEvent
        {
            return @event.DoRemoveHandler(handler);
        }

        [DebuggerStepThrough]
        public static TEvent RemoveHandler<TEvent, TArg>(this TEvent @event, Func<TArg, Task> handler)
            where TEvent : AbstractAsyncEvent
        {
            return @event.DoRemoveHandler(handler);
        }

        [DebuggerStepThrough]
        public static TEvent RemoveHandler<TEvent, TArg1, TArg2>(this TEvent @event, Func<TArg1, TArg2, Task> handler)
           where TEvent : AbstractAsyncEvent
        {
            return @event.DoRemoveHandler(handler);
        }

        [DebuggerStepThrough]
        internal static TEvent DoRemoveHandler<TEvent>(this TEvent @event, Delegate handlerFunction)
            where TEvent : AbstractAsyncEvent
        {
            if (handlerFunction is null) return @event;

            var itemsToRemove = @event.handlers.Where(x => x.Handler == handlerFunction).ToArray();
            if (itemsToRemove.None()) return @event;

            @event.handlers.Remove(itemsToRemove);

            foreach (var handler in itemsToRemove)
                handler.Dispose();

            return @event;
        }

        /// <summary>
        /// The same as Handle. It's added to get past the strange bug in C# for selecting the correct overload of Handle().
        /// </summary> 
        public static TEvent HandleWith<TEvent>(this TEvent @event, Action handler,
           [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
            where TEvent : AbstractAsyncEvent
        {
            return Handle(@event, handler, callerFile, callerLine);
        }

        public static TEvent Handle<TEvent>(this TEvent @event, Action handler,
           [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
            where TEvent : AbstractAsyncEvent
        {
            return HandleOn(@event, null, handler, callerFile, callerLine);
        }

        public static TEvent Handle<TEvent>(this TEvent @event, Func<Task> handler,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
            where TEvent : AbstractAsyncEvent
        {
            return HandleOn(@event, null, handler, callerFile, callerLine);
        }

        /// <summary>
        /// The same as HandleOn. It's added to get past the strange bug in C# for selecting the correct overload of HandleOn().
        /// </summary> 
        public static TEvent HandleActionOn<TEvent>(this TEvent @event, BaseThread thread, Action handler,
             [CallerFilePath] string callerFile = null, [CallerLineNumber] int line = 0)
            where TEvent : AbstractAsyncEvent
        {
            return HandleOn(@event, thread, handler, callerFile, line);
        }

        public static TEvent HandleOn<TEvent>(this TEvent @event, BaseThread thread, Action handler,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int line = 0)
            where TEvent : AbstractAsyncEvent
        {
            if (handler is null) return @event;

            var handlers = @event.GetOrCreateHandlers();
            lock (handlers)
            {
                if (handlers.Any(x => (Delegate)handler == x.Handler)) return @event;

                var eventHandler = new AsyncEventActionHandler
                {
                    Action = handler,
                    Event = @event,
                    Thread = thread,
                    Caller = UIRuntime.IsDebuggerAttached ? callerFile + ":" + line : string.Empty
                };
                handlers.Add(eventHandler);
            }

            return @event;
        }

        /// <summary>
        /// Creates an event handler which you can dispose of explicitly if required.
        /// </summary>
        public static IAsyncEventHandler CreateActionHandler<TEvent>(this TEvent @event, Action handler,
            BaseThread onThread = null,
           [CallerFilePath] string callerFile = null, [CallerLineNumber] int line = 0)
           where TEvent : AbstractAsyncEvent
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            var result = new AsyncEventActionHandler
            {
                Action = handler,
                Event = @event,
                Thread = onThread,
                Caller = UIRuntime.IsDebuggerAttached ? callerFile + ":" + line : string.Empty
            };
            var handlers = @event.GetOrCreateHandlers();
            lock (handlers) handlers.Add(result);

            return result;
        }

        /// <summary>
        /// Creates an event handler which you can dispose of explicitly if required.
        /// </summary>
        public static IAsyncEventHandler CreateHandler<TEvent>(this TEvent @event, Func<Task> handler,
            BaseThread onThread = null,
           [CallerFilePath] string callerFile = null, [CallerLineNumber] int line = 0)
           where TEvent : AbstractAsyncEvent
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            var handlers = @event.GetOrCreateHandlers();
            lock (handlers)
            {
                var result = handlers.FirstOrDefault(x => (Delegate)handler == x.Handler) as IAsyncEventHandler;
                if (result == null)
                {
                    result = new AsyncEventTaskHandler
                    {
                        Action = handler,
                        Event = @event,
                        Thread = onThread,
                        Caller = UIRuntime.IsDebuggerAttached ? callerFile + ":" + line : string.Empty
                    };

                    handlers.Add((AsyncEventHandler)result);
                }

                return result;
            }
        }

        public static TEvent HandleOn<TEvent>(this TEvent @event, BaseThread thread, Func<Task> handler,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int line = 0)
            where TEvent : AbstractAsyncEvent
        {
            if (handler is null) return @event;

            var handlers = @event.GetOrCreateHandlers();

            lock (handlers)
            {
                if (handlers.Any(x => (Delegate)handler == x.Handler)) return @event;

                var item = new AsyncEventTaskHandler
                {
                    Action = handler,
                    Event = @event,
                    Thread = thread,
                    Caller = UIRuntime.IsDebuggerAttached ? callerFile + ":" + line : string.Empty
                };

                handlers.Add(item);
            }

            return @event;
        }
    }
}
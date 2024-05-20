namespace Zebble
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using Olive;

    partial class ViewExtensions
    {
        #region Bindable

        public static TView On<TView, THandlerTarget>(this TView @this, Func<TView, AsyncEvent> @event,
            Bindable<THandlerTarget> target,
            Action<THandlerTarget> handler,
             [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
          where TView : View
        {
            @event(@this).Handle(() => handler(target.Value), callerFile, callerLine);
            return @this;
        }

        public static TView On<TView, TArg, THandlerTarget>(this TView @this, Func<TView, AsyncEvent<TArg>> @event,
            Bindable<THandlerTarget> target, Func<THandlerTarget, Task> handler,
             [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
            where TView : View
        {
            @event(@this).Handle(() => handler(target.Value), callerFile, callerLine);
            return @this;
        }

        public static TView On<TView, TArg, THandlerTarget>(this TView @this, Func<TView, AsyncEvent<TArg>> @event,
            Bindable<THandlerTarget> target,
            Func<THandlerTarget, TArg, Task> handler,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
           where TView : View
        {
            @event(@this).Handle(a => handler(target.Value, a), callerFile, callerLine);
            return @this;
        }

        public static TView On<TView, TArg, THandlerTarget>(this TView @this, Func<TView, AsyncEvent<TArg>> @event,
              Bindable<THandlerTarget> target, Action<THandlerTarget> handler,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
           where TView : View
        {
            @event(@this).Handle(() => handler(target.Value), callerFile, callerLine);
            return @this;
        }

        public static TView On<TView, TArg, THandlerTarget>(this TView @this, Func<TView, AsyncEvent<TArg>> @event,
       Bindable<THandlerTarget> target, Action<THandlerTarget, TArg> handler,
             [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
          where TView : View
        {
            @event(@this).Handle(a => handler(target.Value, a), callerFile, callerLine);
            return @this;
        }

        public static TView On<TView, THandlerTarget>(this TView @this, Func<TView, AsyncEvent> @event,
              Bindable<THandlerTarget> target, Func<THandlerTarget, Task> handler,
             [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
             where TView : View
        {
            @event(@this).Handle(() => handler(target.Value), callerFile, callerLine);
            return @this;
        }

        #endregion

        #region Direct

        public static TView On<TView>(this TView @this, Func<TView, AsyncEvent> @event, Action handler,
             [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
          where TView : View
        {
            @event(@this).Handle(handler, callerFile, callerLine);
            return @this;
        }

        public static TView On<TView, TArg>(this TView @this, Func<TView, AsyncEvent<TArg>> @event, Func<Task> handler,
             [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
            where TView : View
        {
            @event(@this).Handle(handler, callerFile, callerLine);
            return @this;
        }

        public static TView On<TView, TArg>(this TView @this, Func<TView, AsyncEvent<TArg>> @event, Func<TArg, Task> handler,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
           where TView : View
        {
            @event(@this).Handle(handler, callerFile, callerLine);
            return @this;
        }

        public static TView On<TView, TArg>(this TView @this, Func<TView, AsyncEvent<TArg>> @event, Action handler,
            [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
           where TView : View
        {
            @event(@this).Handle(handler, callerFile, callerLine);
            return @this;
        }

        public static TView On<TView, TArg>(this TView @this, Func<TView, AsyncEvent<TArg>> @event, Action<TArg> handler,
             [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
          where TView : View
        {
            @event(@this).Handle(handler, callerFile, callerLine);
            return @this;
        }

        public static TView On<TView>(this TView @this, Func<TView, AsyncEvent> @event, Func<Task> handler,
             [CallerFilePath] string callerFile = null, [CallerLineNumber] int callerLine = 0)
             where TView : View
        {
            @event(@this).Handle(handler, callerFile, callerLine);
            return @this;
        }

        #endregion
    }
}
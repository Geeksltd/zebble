namespace Zebble
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    partial class View
    {
        bool? autoFlash;

        public bool? AutoFlash
        {
            get => autoFlash;
            set
            {
                autoFlash = value;
                Touched.RemoveHandler(DoAutoFlash);
                if (value ?? true) Touched.Handle(DoAutoFlash);
            }
        }

        bool ShouldFlash
        {
            get
            {
                if (autoFlash.HasValue) return autoFlash.Value;
                return HasAnyHandlerExceptDoAutoFlash(Touched) || HasAnyHandlerExceptDoAutoFlash(Tapped);
            }
        }

        bool HasAnyHandlerExceptDoAutoFlash(AsyncEvent<TouchEventArgs> @event)
        {
            if (@event.IsHandled()) return true;
            return @event.handlers?.Any(x => !ReferenceEquals(x.Handler, (Func<Task>)DoAutoFlash)) == true;
        }

        void InitializeAutoFlash()
        {
            var respondsToTap = HasAnyHandlerExceptDoAutoFlash(Tapped);
            var respondsToTouch = HasAnyHandlerExceptDoAutoFlash(Touched);

            if (!respondsToTap && !respondsToTouch) return;

            var alreadyAdded = Touched.handlers.OrEmpty()
                .Concat(Tapped.handlers.OrEmpty())
                .Any(x => ReferenceEquals(x.Handler, (Func<Task>)DoAutoFlash));
            if (alreadyAdded) return;

            var @event = respondsToTouch ? Touched : Tapped;
            @event.GetOrCreateHandlers().Insert(0, new AsyncEventTaskHandler { Action = DoAutoFlash });
        }

        Task DoAutoFlash() => Enabled && ShouldFlash ? Flash() : Task.CompletedTask;
    }
}
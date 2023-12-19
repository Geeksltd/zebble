using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Olive;

namespace Zebble.Mvvm
{
    public abstract partial class ViewModel
    {
        public static readonly Bindable<DateTime> NavAnimationStarted = new();

        public readonly static Stack<ViewModel> Stack = new();

        /// <summary>
        /// Gets the current full screen page.
        /// </summary>
        public static FullScreen Page { get; internal set; }

        /// <summary>
        /// Gets all modal screens on the page.
        /// </summary>
        public static Stack<ModalScreen> Modals { get; } = new();

        /// <summary>
        /// Gets the top most modal screen on the page.
        /// </summary>
        public static ModalScreen Modal => Modals.TryPeek(out var modal) ? modal : null;

        /// <summary>
        /// Gets the current modal (if open) or otherwise, the current page.
        /// </summary>
        public static ViewModel ActiveScreen => Modal ?? (ViewModel)Page;

        public static Task HidePopUp() => new ViewModelNavigation(null, PageTransition.DropUp).HidePopUp();

        public static bool CanGoBack() => Stack.Any();

        public static Task Back(PageTransition transition = PageTransition.SlideBack)
        {
            if (Stack.None()) throw new Exception("There is no previous page in the stack to go back to.");

            return new ViewModelNavigation((FullScreen)Stack.Pop(), transition).Back();
        }

        public static Task Go(FullScreen target, PageTransition transition = PageTransition.SlideForward)
        {
            Stack.Clear();

            return new ViewModelNavigation(target, transition).Go();
        }

        public static Task Forward(FullScreen target, PageTransition transition = PageTransition.SlideForward)
        {
            if (Page != null) Stack.Push(Page);

            return new ViewModelNavigation(target, transition).Forward();
        }

        public static Task Replace(FullScreen target, PageTransition transition = PageTransition.SlideForward)
        {
            if (Stack.None()) throw new Exception("There is no previous page in the stack to be peeked out.");
            Page = (FullScreen)Stack.Peek();

            return new ViewModelNavigation(target, transition).Replace();
        }

        public static Task Reload() => ViewModelNavigation.Reload();

        public static Task ShowPopUp(ModalScreen target, PageTransition transition = PageTransition.DropUp)
        {
            return new ViewModelNavigation(target, transition).ShowPopUp();
        }

        public static Task Go<TDestination>(PageTransition transition = PageTransition.SlideForward)
            where TDestination : FullScreen
        {
            return Go(The<TDestination>(), transition);
        }

        public static Task Forward<TDestination>(PageTransition transition = PageTransition.SlideForward)
           where TDestination : FullScreen
        {
            return Forward(The<TDestination>(), transition);
        }

        public static Task Replace<TDestination>(PageTransition transition = PageTransition.SlideForward)
           where TDestination : FullScreen
        {
            return Replace(The<TDestination>(), transition);
        }

        public static Task ShowPopUp<TDestination>(PageTransition transition = PageTransition.DropUp) where TDestination : ModalScreen
        {
            return ShowPopUp(The<TDestination>(), transition);
        }

        protected internal virtual void NavigationStarted()
        {
            var bindables = GetType()
                .GetPropertiesAndFields(BindingFlags.Public | BindingFlags.Instance)
               .Where(c => c.GetPropertyOrFieldType().IsA<Bindable>()).ToArray();

            foreach (var bindableProperty in bindables)
            {
                var bindable = bindableProperty.GetValue(this) as Bindable;
                if (bindable is null) continue;
                var changedEvent = bindable.GetType().GetEvent("Changed");
                if (changedEvent is null) continue;
                changedEvent.AddEventHandler(bindable, () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(bindableProperty.Name)));
            }
        }

        protected internal virtual void NavigationCompleted() { }

        protected internal virtual Task NavigationStartedAsync() => Task.CompletedTask;

        protected internal virtual Task NavigationCompletedAsync() => Task.CompletedTask;

        protected internal virtual void LeaveStarted() { }

        protected internal virtual void LeaveCompleted() { }
    }
}
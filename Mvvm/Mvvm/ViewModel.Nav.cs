using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Olive;

namespace Zebble.Mvvm
{
    public abstract partial class ViewModel
    {
        public static readonly Bindable<DateTime> NavAnimationStarted = new Bindable<DateTime>();

        static Func<ModalScreen, Task> RealShowPopup;

        public readonly static Stack<ViewModel> Stack = new Stack<ViewModel>();

        /// <summary>
        /// Gets the current full screen page.
        /// </summary>
        public static FullScreen Page { get; internal set; }

        /// <summary>
        /// Gets the current modal screen on the page.
        /// </summary>
        public static ModalScreen Modal { get; internal set; }

        /// <summary>
        /// Gets the current modal (if open) or otherwise, the current page.
        /// </summary>
        public static ViewModel ActiveScreen => Modal ?? (ViewModel)Page;

        public static void HidePopUp() => new ViewModelNavigation(null, PageTransition.DropUp).HidePopUp();

        public static void Back(PageTransition transition = PageTransition.SlideBack)
        {
            if (Stack.None()) throw new Exception("There is no previous page in the stack to go back to.");
            new ViewModelNavigation((FullScreen)Stack.Pop(), transition).Back();
        }

        public static void Go(FullScreen target, PageTransition transition = PageTransition.SlideForward)
        {
            Stack.Clear();
            new ViewModelNavigation(target, transition).Go();
        }

        public static void Forward(FullScreen target, PageTransition transition = PageTransition.SlideForward)
        {
            if (Page != null) Stack.Push(Page);
            new ViewModelNavigation(target, transition).Forward();
        }

        public static void Replace(FullScreen target, PageTransition transition = PageTransition.SlideForward)
        {
            if (Stack.None()) throw new Exception("There is no previous page in the stack to be peeked out.");
            Page = (FullScreen)Stack.Peek();
            new ViewModelNavigation(target, transition).Replace();
        }

        public static void ShowPopUp(ModalScreen target, PageTransition transition = PageTransition.DropUp)
        {
            if (Modal != null) HidePopUp();

            new ViewModelNavigation(target, transition).ShowPopUp();
        }

        public static void Go<TDestination>(PageTransition transition = PageTransition.SlideForward)
            where TDestination : FullScreen
        {
            Go(The<TDestination>(), transition);
        }

        public static void Forward<TDestination>(PageTransition transition = PageTransition.SlideForward)
           where TDestination : FullScreen
        {
            Forward(The<TDestination>(), transition);
        }

        public static void Replace<TDestination>(PageTransition transition = PageTransition.SlideForward)
           where TDestination : FullScreen
        {
            Replace(The<TDestination>(), transition);
        }

        public static void ShowPopUp<TDestination>(PageTransition transition = PageTransition.DropUp) where TDestination : ModalScreen
        {
            ShowPopUp(The<TDestination>(), transition);
        }

        protected internal virtual void NavigationStarted() { }

        protected internal virtual void NavigationCompleted() { }

        protected internal virtual Task NavigationStartedAsync() => Task.CompletedTask;

        protected internal virtual Task NavigationCompletedAsync() => Task.CompletedTask;

        protected internal virtual void LeaveStarted() { }

        protected internal virtual void LeaveCompleted() { }
    }
}
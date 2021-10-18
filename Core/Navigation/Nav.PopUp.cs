namespace Zebble
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    partial class Nav
    {
        static AsyncLock PopupLock = new AsyncLock();
        static readonly List<PopUp> CachedPopups = new List<PopUp>();

        internal static ConcurrentList<PopUp> PopUps { get; } = new ConcurrentList<PopUp>();

        public static Task<TResult> ShowPopUp<TPage, TResult>(object navParams = null) where TPage : Page
        {
            return ShowPopUp<TPage, TResult>(Activator.CreateInstance<TPage>(), navParams);
        }

        public static Task ShowPopUp<TPage>(object navParams = null) where TPage : Page
        {
            return ShowPopUp(Activator.CreateInstance<TPage>(), navParams);
        }

        public static Task<TResult> ShowPopUp<TView, TResult>(TView view, PageTransition transition) where TView : View
        {
            return ShowPopUp<TView, TResult>(view, null, transition);
        }

        public static Task ShowPopUp<TView>(TView view, PageTransition transition) where TView : View
        {
            return ShowPopUp(view, null, transition);
        }

        public static Task<TResult> ShowPopUp<TView, TResult>(TView view, object navParams = null, PageTransition transition = PageTransition.DropUp) where TView : View
        {
            return PopupListener.Show(() => DoShowPopUp<TView, TResult>(view, navParams, transition));
        }

        /// <summary>
        /// Loads a page in a pop-up container on top of the current page.
        /// It won't change the current navigation stack.
        /// </summary>
        public static Task ShowPopUp<TView>(TView view, object navParams = null, PageTransition transition = PageTransition.DropUp)
            where TView : View
        {
            return PopupListener.Show(() => DoShowPopUp<TView, bool>(view, navParams, transition, awaitResult: false));
        }

        static async Task<TResult> DoShowPopUp<TView, TResult>(TView view, object navParams = null, PageTransition transition = PageTransition.DropUp, bool awaitResult = true) where TView : View
        {
            UnfocusAll();

            PopUp popup;
            using (await PopupLock.Lock())
            {
                var waitingVersion = Waiting.ShownVersion;
                UnfocusAll();

                // From cache:
                popup = CachedPopups.FirstOrDefault(x => x.GetView().GetType() == view.GetType());
                var revisiting = popup != null;

                if (popup is null)
                {
                    popup = new PopUp<TView, TResult>(view, CurrentPage);

                    if (CacheViewAttribute.IsCacheable(popup))
                        CachedPopups.Add(popup);
                }
                else if (popup != CurrentPage)
                {
                    popup.HostPage = CurrentPage;
                }

                popup.Transition = transition;
                popup.NavParams = SerializeToDictionary(navParams);

                if (revisiting)
                    popup.OnRevisiting.RaiseOn(Thread.Pool, new RevisitingEventArgs()).RunInParallel();

                await AddPopUp(popup, transition);
                await Waiting.Hide(waitingVersion);

                if (revisiting)
                    await popup.OnRevisited.Raise(new RevisitingEventArgs());
            }

            if (awaitResult)
                return await ((IPopupWithResult<TResult>)popup).ResultTask.Task;
            else
                return default(TResult);
        }

        static async Task AddPopUp(PopUp popup, PageTransition transition)
        {
            popup.Transition = transition;
            var eventArgs = new NavigationEventArgs(popup.HostPage, popup);
            await Navigating.Raise(eventArgs);

            lock (TransitionStack)
            {
                IsNavigating = true;
                TransitionStack.Push(popup.Transition);
                PopUps.Add(popup);
                CurrentPage = popup;
            }

            if (popup.Parent is null)
                await UIWorkBatch.Run(() => View.Root.Add(popup, awaitNative: true));
            else await popup.BringToFront();
        }

        static void UnfocusAll()
        {
            var unfocus = View.Root
                .WithAllDescendants()
                .OfType<TextInput>()
                .Where(t => t.Focused.Value)
                .Except(x => x.IsDisposing)
                .Except(x => x.Ignored)
                .Where(x => x.IsShown)
                .ToArray();

            foreach (var tb in unfocus)
            {
                try { tb.UnFocus(); }
                catch (Exception ex) { Log.For<Nav>().Error(ex, "Failed to unfocus a text input!"); }
            }
        }

        public static Task HidePopUp() => PopupListener.Hide(() => HidePopUp(result: false));

        public static async Task<TResult> HidePopUp<TResult>(TResult result)
        {
            UnfocusAll();

            using (await PopupLock.Lock())
            {
                UnfocusAll();

                PopUp top;
                lock (TransitionStack) top = PopUps.LastOrDefault();
                if (top != null)
                {
                    if (top is IPopupWithResult<TResult> typed)
                    {
                        typed.SetResult(result);
                    }
                    else if (top is IPopupWithResult untyped)
                    {
                        if (result.ToStringOrEmpty() == false.ToString()) untyped.SetDefaultResult();
                        else throw new Exception("The expected result type of " + top.GetType().GetProgrammingName() +
                            " is not " + typeof(TResult).GetProgrammingName());
                    }

                    await RemovePopUp(top);
                }
                else if (CurrentPage is PopUp popup)
                {
                    CurrentPage = popup.HostPage;

                    if (CurrentPage?.IsDisposing == true)
                        Log.For<Nav>().Warning("Current page is disposing!!! " + CurrentPage.GetType().Name);
                }

                return result;
            }
        }

        static async Task RemovePopUp(PopUp popup)
        {
            var eventArgs = new NavigationEventArgs(popup, popup.HostPage);
            await Navigating.Raise(eventArgs);
            IsNavigating = true;

            PageTransition transition;

            lock (TransitionStack)
            {
                PopUps.Remove(popup);
                transition = TransitionStack.Any() ? TransitionStack.Pop().GetReverse() : CurrentPage.Transition.GetReverse();

                popup.Hide(transition).ContinueWith(x => onAnimationCompleted()).GetAwaiter(); // Animate in parallel

                if (CurrentPage == popup)
                {
                    CurrentPage = popup.HostPage;
                    if (CurrentPage?.IsDisposing == true)
                        Log.For<Nav>().Warning("Current page is disposing!!! " + CurrentPage.GetType().Name);
                }
            }

            async Task onAnimationCompleted()
            {
                IsNavigating = false;
                await Navigated.Raise(eventArgs);

                if (!CacheViewAttribute.IsCacheable(popup))
                    await popup.RemoveSelf();
            }
        }
    }
}
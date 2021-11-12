namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Olive;

    public partial class Nav
    {
        const int DROP_RANGE = 30;
        public static Stack<Page> Stack { get; } = new();
        public static Stack<PageTransition> TransitionStack = new();
        static readonly List<Page> CachedPages = new();
        public static Page CurrentPage { get; set; }
        public static bool IsNavigating { get; internal set; }

        public static readonly AsyncEvent FullRefreshed = new();

        /// <summary>
        /// Raised before even the target page is necessarily rendered.
        /// </summary>
        public static readonly AsyncEvent<NavigationEventArgs> Navigating = new();

        /// <summary>
        /// Raised when the target page is rendered and added to the UI tree, and the transition animation has just started.
        /// </summary>
        public static readonly AsyncEvent<NavigationEventArgs> NavigationAnimationStarted = new();

        /// <summary>
        /// Raised after the transition animation is completed.
        /// </summary>
        public static readonly AsyncEvent<NavigationEventArgs> Navigated = new();

        /// <summary>
        /// Removes and disposes all currently cached pages.
        /// </summary>
        public static void DisposeCache() => DisposeCache(null);

        /// <summary>
        /// Removes and disposes all currently cached pages of the specified type.
        /// </summary>
        public static void DisposeCache(Type pageType)
        {
            foreach (var item in CachedPages.Concat(CachedPopups).ToArray())
            {
                if (!item.IsDisposing && CurrentPage != item)
                {
                    if (pageType != null && !item.GetType().IsA(pageType)) continue;
                    try { item.Dispose(); }
                    catch (Exception ex) { Log.For<Nav>().Error(ex); }
                }

                CachedPages.Remove(item);

                if (item is PopUp p) CachedPopups.Remove(p);
            }
        }

        public static Page GetFromCache(Type pageType) => CachedPages.FirstOrDefault(x => x.GetType() == pageType);

        static bool IsCachedPage(Page page) => GetFromCache(page?.GetType()) == page;

        public static T Param<T>(string key)
        {
            var parameters = PopUps.LastOrDefault()?.NavParams ?? CurrentPage?.NavParams;

            if (parameters?.Keys.Contains(key, caseSensitive: false) != true) return default;

            var paramValue = parameters.FirstOrDefault(p => p.Key.Equals(key, caseSensitive: false)).Value;

            if (paramValue is null) return default;

            if (paramValue is T value) return value;

            throw new InvalidCastException($"Nav.Param('{key}') is of type '{paramValue?.GetType().Name}' and not convertible to '{typeof(T).Name}'.");
        }

        static async Task RaiseNavigating(NavigationEventArgs args)
        {
            if (args.From != null && !(args.To is PopUp))
                await args.From.OnExiting.Raise(args);
            await Navigating.Raise(args);
        }

        static async Task RaiseNavigated(NavigationEventArgs args)
        {
            if (args.From != null && !(args.To is PopUp))
                await args.From.OnExited.Raise(args);
            await Navigated.Raise(args);
        }

        static async Task Navigate(Page page, RevisitingEventArgs revisitedArgs)
        {
            using (SuspendGC.Start())
                await DoNavigate(page, revisitedArgs);
        }

        static async Task DoNavigate(Page page, RevisitingEventArgs revisitedArgs)
        {
            UnfocusAll();

            while (PopUps.Any(x => x.IsFullyVisible)) await HidePopUp();

            var waitingVersion = Waiting.ShownVersion;

            var eventArgs = new NavigationEventArgs(CurrentPage, page);
            await RaiseNavigating(eventArgs);

            IsNavigating = true;
            try
            {
                var oldPage = CurrentPage;
                CurrentPage = page;

                if (CacheViewAttribute.IsCacheable(page) && GetFromCache(page.GetType()) != page)
                {
                    CachedPages.RemoveWhere(x => x.GetType() == page.GetType());
                    CachedPages.Add(page);
                }

                if (oldPage is null) await View.Root.Add(page);
                else if (oldPage != page) await oldPage.NavigateTo(page);

                // Undo the past animation.
                if (page.Transition == PageTransition.None || page.Transition == PageTransition.Fade)
                    UIWorkBatch.RunSync(() => page.X(0).Y(0));

                AddToHistory(page);
            }
            finally
            {
                IsNavigating = false;
            }

            await Waiting.Hide(waitingVersion);

            if (revisitedArgs != null)
                await page.OnRevisited.Raise(revisitedArgs);



            await RaiseNavigated(eventArgs);
        }

        /// <summary>Redirects to the specified page.</summary>
        public static Task Go<TPage>(PageTransition transition) where TPage : Page => Go(typeof(TPage), null, transition);

        /// <summary>Redirects to the specified page.</summary>
        public static Task Go(Type pageType, PageTransition transition) => Go(pageType, null, transition);

        /// <summary>Redirects to the specified page.</summary>
        /// <param name="navParams">Provide either an IDictionary[string, object] or an anonymous object.</param>
        public static Task Go<TPage>(object navParams = null, PageTransition transition = PageTransition.SlideForward) where TPage : Page
        {
            return Go(typeof(TPage), navParams, transition);
        }

        /// <summary>Redirects to the specified page.</summary>
        /// <param name="navParams">Provide either an IDictionary[string, object] or an anonymous object.</param>
        public static Task Go(Type pageType, object navParams = null, PageTransition transition = PageTransition.SlideForward)
        {
            var temp = GetOrCreateTargetPage(pageType);
            return Go(temp.Item1, navParams, transition, clearStack: true, revisiting: temp.Item2);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task FullRefresh()
        {
            await FullRefreshed.Raise();
            await Reload();
        }

        /// <summary>Reloads the current page.</summary>
        public static async Task Reload()
        {
            if (CurrentPage is null) return;
            CachedPages.RemoveWhere(x => x.GetType() == CurrentPage.GetType());

            var currentParams = CurrentPage.NavParams;

            if (CurrentPage is PopUp)
            {
                var pageType = CurrentPage.GetType().GetGenericArguments().FirstOrDefault();
                if (pageType is null) return; // high concurrency issue

                if (pageType.IsA<Page>() && !(CurrentPage is PopUp))
                {
                    var showPopupMethod = typeof(Nav).GetMethods()
                             .Where(m => m.Name == "ShowPopUp")
                             .Single(m => m.GetParameters().IsSingle())
                             .MakeGenericMethod(pageType);

                    await HidePopUp();
                    showPopupMethod.Invoke(null, new[] { currentParams });
                }
                else
                {
                    await HidePopUp();
                    await Reload();
                }
            }
            else
            {
                var newPage = CurrentPage.GetType().CreateInstance<Page>();
                await Go(newPage, currentParams, PageTransition.None, clearStack: false);
            }
        }

        /// <summary>Redirects to the specified page.</summary>
        public static async Task Go(Page page, PageTransition transition) => await Go(page, null, transition);

        /// <summary>Redirects to the specified page.</summary>
        /// <param name="navParams">Provide either an IDictionary[string, object] or an anonymous object.</param>
        public static Task Go(Page page, object navParams = null, PageTransition transition = PageTransition.SlideForward, bool clearStack = true)
            => Go(page, navParams, transition, clearStack, IsCachedPage(page));

        static async Task Go(Page page, object navParams, PageTransition transition, bool clearStack, bool revisiting)
        {
            if (clearStack) Stack.Clear();

            Page oldPageToKill = null;
            var inCache = GetFromCache(CurrentPage?.GetType());
            // It's not cached, or it's a different instance from the new page. So release the memory.
            if (inCache == null || (inCache != page && page.GetType() == inCache.GetType()))
                oldPageToKill = CurrentPage;

            if (CurrentPage is PopUp p && CacheViewAttribute.IsCacheable(p.GetView() as Page))
                oldPageToKill = null;

            var newNavParams = SerializeToDictionary(navParams);

            RevisitingEventArgs args = null;

            if (revisiting)
            {
                if (EqualsValue(newNavParams, page.NavParams))
                    args = new RevisitingEventArgs
                    {
                        NavParams = newNavParams,
                        Mode = RevisitMode.SameParams
                    };

                else
                    args = new RevisitingEventArgs
                    {
                        NavParams = newNavParams,
                        Mode = RevisitMode.NewParams
                    };

                if (page.OnRevisiting.IsHandled())
                    page.OnRevisiting.RaiseOn(Thread.Pool, args).RunInParallel();
            }

            page.Transition = transition;
            page.NavParams = newNavParams;

            await Navigate(page, args);

            if (oldPageToKill != null)
                await Thread.Pool.Run(() => DisposeGonePage(oldPageToKill));
        }

        static bool EqualsValue(IDictionary<string, object> first, IDictionary<string, object> second)
        {
            // null object equals with empty object.

            if (second is null)
                return first.None();

            if (first is null) return second.None();

            if (ReferenceEquals(first, second)) return true;

            if (first.Count != second.Count) return false;

            // check keys are the same
            foreach (var key in first.Keys)
                if (!second.ContainsKey(key)) return false;

            // check values are the same
            foreach (var key in first.Keys)
                if (!first[key].Equals(second[key])) return false;

            return true;
        }

        static async Task DisposeGonePage(Page oldPageToKill)
        {
            if (UIRuntime.IsProfilingPerformance) return; // Remove disposing time from measurements.

            await Task.Delay(500.Milliseconds()); // To ensure animations on the page are completed.

            var description = UIRuntime.IsDevMode ? ("Dispose " + oldPageToKill) : "Dispose";

            Services.IdleUITasks.Run(description, () =>
            {
                if (CurrentPage != oldPageToKill)
                    oldPageToKill?.Dispose();
                GC.Collect(0);
            });
        }

        /// <summary>This is the same as Go() but with the reverse transition. </summary>
        public static Task GoBack<TPage>(object navParams = null) => GoBack(typeof(TPage), navParams);

        /// <summary>This is the same as Go() but with the reverse transition. </summary>
        public static Task GoBack(Type pageType, object navParams = null)
        {
            var temp = GetOrCreateTargetPage(pageType);
            return GoBack(temp.Item1, navParams);
        }

        /// <summary>This is the same as Go() but with the reverse transition. </summary>
        public static Task GoBack(Page page, object navParams = null)
        {
            return Go(page, navParams, CurrentPage.Transition.GetReverse(), clearStack: true, revisiting: IsCachedPage(page));
        }

        /// <summary>The same as Go() but it also adds the page to the Stack to enable «going back».</summary>
        public static Task Forward<TPage>(PageTransition transition) where TPage : Page => Forward(typeof(TPage), null, transition);

        /// <summary>The same as Go() but it also adds the page to the Stack to enable «going back».</summary>
        public static Task Forward(Type pageType, PageTransition transition) => Forward(pageType, null, transition);

        /// <summary>The same as Go() but it also adds the page to the Stack to enable «going back».</summary>
        /// <param name="navParams">Provide either an IDictionary[string, object] or an anonymous object.</param>
        public static Task Forward<TPage>(object navParams = null, PageTransition transition = PageTransition.SlideForward)
            where TPage : Page
        {
            return Forward(typeof(TPage), navParams, transition);
        }

        /// <summary>The same as Go() but it also adds the page to the Stack to enable «going back».</summary>
        /// <param name="navParams">Provide either an IDictionary[string, object] or an anonymous object.</param>
        public static Task Forward(Type pageType, object navParams = null, PageTransition transition = PageTransition.SlideForward)
        {
            var temp = GetOrCreateTargetPage(pageType);
            return Forward(temp.Item1, navParams, transition, temp.Item2);
        }

        /// <summary>
        /// It tries to load the page from cache.<para/>
        /// The Item1 of the result is the required page and the Item2 shows that whether it is a cached page or not.
        /// </summary>
        static Tuple<Page, bool> GetOrCreateTargetPage(Type pageType)
        {
            var cachedPage = GetFromCache(pageType);

            // If it is not cached, create a new instance of it
            if (cachedPage is null)
                return Tuple.Create(pageType.CreateInstance<Page>(), item2: false);

            return Tuple.Create(cachedPage, item2: true);
        }

        /// <summary>The same as Go() but it also adds the page to the Stack to enable «going back».</summary>
        /// <param name="navParams">Provide either an IDictionary[string, object] or an anonymous object.</param>
        public static Task Forward(Page page, object navParams = null, PageTransition transition = PageTransition.SlideForward)
            => Forward(page, navParams, transition, IsCachedPage(page));

        static async Task Forward(Page page, object navParams, PageTransition transition, bool revisiting)
        {
            while (CurrentPage is PopUp) await HidePopUp();

            lock (TransitionStack)
                CurrentPage?.Perform(x => { Stack.Push(x); TransitionStack.Push(transition); });

            await Go(page, navParams, transition, clearStack: false, revisiting: revisiting);
        }

        /// <summary>
        /// Removes the current page from the stack, and navigates to the previous page in the stack.
        /// This should be called after a Forward() call in scenarios where the navigation is in one journey that has back and forward concept.
        /// </summary>
        public static async Task Back()
        {
            if (Stack.None()) throw new Exception("There is no previous page in the stack to go back to.");

            var previousPage = Stack.Pop();

            PageTransition transision;
            lock (TransitionStack)
            {
                transision = TransitionStack.Any() ?
                    TransitionStack.Pop().GetReverse() :
                    CurrentPage.Transition.GetReverse();
            }

            var destinationPage = GetFromCache(previousPage.GetType());
            var revisition = true;

            if (destinationPage is null)
            {
                destinationPage = previousPage.GetType().CreateInstance<Page>();
                revisition = false;
            }

            // This page is not cache-able. However it was rendered once before. So recreate it before rendering.
            await Go(destinationPage, previousPage.NavParams, transision, clearStack: false, revisiting: revisition);
        }

        /// <summary>
        /// Warmp-up cachable target page to enable to navigate faster.
        /// </summary>
        public static async Task WarmUp<TPage>() where TPage : Page
        {
            if (GetFromCache(typeof(TPage)) != null) return;

            var page = GetOrCreateTargetPage(typeof(TPage)).Item1;

            if (CacheViewAttribute.IsCacheable(page))
            {
                CachedPages.Add(page);
                await View.Root.Add(page.X(View.Root.ActualWidth).Y(0));
            }
            else
            {
                Log.For<Nav>().Error("The WarmUp navigation just work on cachable pages, please make sure the target page has the CacheViewAttribute");
            }
        }

        static IDictionary<string, object> SerializeToDictionary(object navParams)
        {
            var result = new Dictionary<string, object>();

            if (navParams is null) return result;

            if (navParams.GetType().Name.StartsWith("Dictionary")) return (IDictionary<string, object>)navParams;

            foreach (var p in navParams.GetType().GetPropertiesAndFields(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance))
                result[p.Name] = p.GetValue(navParams);

            return result;
        }
    }
}
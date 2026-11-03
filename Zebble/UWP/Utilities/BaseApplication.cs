namespace Zebble.UWP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Activation;
    using Windows.UI.Core;
    using Windows.UI.ViewManagement;
    using controls = Windows.UI.Xaml.Controls;
    using xaml = Windows.UI.Xaml;
    using Olive;

    public abstract partial class BaseApplication : xaml.Application
    {
        public static float MinWidth = 320;
        public static float MinHeight = 320;
        public static float InitialWidth = 375;
        public static float InitialHeight = 675;
        static ApplicationView appView;
        static bool IsRefreshing, IsLaunched;

        static DateTime LastSizeChanged;

        static xaml.Window Window => xaml.Window.Current;

        protected BaseApplication()
        {
            UIThread.UIThreadID = Environment.CurrentManagedThreadId;
            Windows.System.MemoryManager.AppMemoryUsageIncreased += MemoryManager_AppMemoryUsageIncreased;
            EnteredBackground += (_, __) => Device.App.RaiseWentIntoBackground();
            LeavingBackground += (_, __) => Device.App.RaiseCameToForeground();
            Suspending += OnSuspending;
        }

        void MemoryManager_AppMemoryUsageIncreased(object sender, object eventArgs)
        {
            if (!IsLaunched) return;
            var level = Windows.System.MemoryManager.AppMemoryUsageLevel;

            if ((int)level < (int)Windows.System.AppMemoryUsageLevel.High) return;

            Device.App.RaiseReceivedMemoryWarning();
        }

        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            UIThread.Dispatcher = Window.Dispatcher;

            base.OnLaunched(args);

            await HandleArguments(args.Arguments);

            if (args.PreviousExecutionState == ApplicationExecutionState.Running) return;

            Setup.Start();

            // Pre-load the screen density on the UI thread:
            Device.Screen.Density.ToString();
            Device.Screen.HardwareDensity.ToString();
            Device.Screen.SafeAreaInsets.UpdateValues();

            await SetInitialWindowSize();
            await CompleteScreen();

            IsLaunched = true;
            Window.SizeChanged += Window_SizeChanged;
        }

        async void Window_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            var myVersion = LastSizeChanged = LocalTime.UtcNow;
            await Task.Delay(1.Seconds());
            if (LastSizeChanged != myVersion) return;

            while (IsRefreshing)
                await Task.Delay(10);

            IsRefreshing = true;

            UpdateSize();

            try
            {
                Nav.DisposeCache();
                UIRuntime.SkipPageRefresh = true;
                await Nav.FullRefresh();
                UIRuntime.SkipPageRefresh = false;
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        async Task HandleArguments(string args)
        {
            if (args.OrNullIfEmpty() != null)
            {
                var info = new Dictionary<string, string>();
                var arguments = args.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in arguments)
                {
                    var keyAndValue = item.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                    if (keyAndValue.Any())
                        info.Add(keyAndValue[0], keyAndValue[1]);
                }

                await UIRuntime.OnParameterRecieved.Raise(info);
            }

            await Task.CompletedTask;
        }

        async Task CompleteScreen()
        {
            if (UIRuntime.IsDevMode) await UIRuntime.Inspector.PrepareRuntimeRoot();

            await CreateRootFrame();

            DebugSettings.EnableFrameRateCounter = System.Diagnostics.Debugger.IsAttached;

            SystemNavigationManager.GetForCurrentView().BackRequested += async (s, e) =>
            {
                if (Nav.CanGoBack())
                {
                    e.Handled = true;
                    await Nav.OnHardwareBack();
                }
            };
            await (StartUp.Current = CreateStartUp()).Run();

            if (UIRuntime.IsDevMode) LiveCssWatcher.Start().RunInParallel();
            Window.Activate();
            SetTitle();
        }

        async Task CreateRootFrame()
        {
            var rootFrame = Window.Content as controls.Frame;

            if (rootFrame is null)
                Window.Content = rootFrame = new controls.Frame
                {
                    Width = Device.Screen.Width,
                    Height = Device.Screen.Height
                };

            if (rootFrame.Content != null)
            {
                // Already launched
                return;
            }

            rootFrame.Content = (await UIRuntime.RenderRoot.Render()).Native;
        }

        protected abstract StartUp CreateStartUp();

        static ApplicationView AppView => appView ?? (appView = ApplicationView.GetForCurrentView());

        static async void SetTitle()
        {
            while (StartUp.ApplicationName.IsEmpty())
                await Task.Delay(200);

            AppView.Title = StartUp.ApplicationName;
        }

        async Task SetInitialWindowSize()
        {
            Device.Screen.density = null;

            // AppView.VisibleBoundsChanged += (s, ee) => UpdateSize();

            if (Device.App.IsDesktop())
            {
                var minSize = new Windows.Foundation.Size(MinWidth, MinHeight);
                var initialSize = new Windows.Foundation.Size(InitialWidth, InitialHeight);

                AppView.SetPreferredMinSize(minSize);
                AppView.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);

                ApplicationView.PreferredLaunchViewSize = initialSize;
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
                ApplicationView.GetForCurrentView().TryResizeView(initialSize);
            }

            UpdateSize();
        }

        static float FindScreenWidth()
        {
            var result = Device.Screen.Width;
            var inspectorWidth = UIRuntime.IsDevMode ? UIRuntime.Inspector.CurrentWidth : 0;

            if (Device.Screen.Width != 0 && inspectorWidth == 0)
                result = (float)Window.Bounds.Width;
            else
                result = (float)AppView.VisibleBounds.Width - inspectorWidth;

            if (result < MinWidth)
            {
                result = MinWidth;
                UIRuntime.Inspector.Collapse().GetAwaiter();
            }

            return result;
        }

        static float FindScreenHeight()
        {
            var result = (float)AppView.VisibleBounds.Height;

            if (!Device.App.IsDesktop()) result -= (float)AppView.VisibleBounds.Top;

            return result;
        }

        public static void UpdateSize()
        {
            Device.Screen.density = null;

            Device.Screen.ConfigureSize(FindScreenWidth, FindScreenHeight);

            (Window.Content as controls.Frame).Perform(x =>
            {
                x.Height = AppView.VisibleBounds.Height;
                x.Width = AppView.VisibleBounds.Width;
            });

            Device.Screen.OrientationChanged.SignalRaiseOn(Thread.Pool);
        }

        void OnSuspending(object sender, SuspendingEventArgs args)
        {
            LiveCssWatcher.Exit();

            var deferral = args.SuspendingOperation.GetDeferral();
            Device.App.RaiseWentIntoBackground();
            Device.App.RaiseStopping();
            deferral.Complete();
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Launch)
            {
                var launchArgs = (LaunchActivatedEventArgs)args;
                HandleArguments(launchArgs.Arguments).GetAwaiter();
            }

            UIRuntime.OnActivated?.Raise(Tuple.Create(args, Window));
        }
    }
}
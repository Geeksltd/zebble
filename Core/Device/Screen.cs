namespace Zebble.Device
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Threading.Tasks;
    using Olive;

    public static partial class Screen
    {
        public static partial class SafeAreaInsets
        {
            public static float Top { get; internal set; }
            public static float Right { get; internal set; }
            public static float Bottom { get; internal set; }
            public static float Left { get; internal set; }

            public static void UpdateValues() => DoUpdateValues();
        }

        public static partial class StatusBar
        {
            public static float Height;

            static Color backgroundColor;
            public static Color BackgroundColor
            {
                get => backgroundColor;
                set
                {
                    if (backgroundColor == value) return;
                    backgroundColor = value;
                    SetBackgroundColor();
                }
            }

            static Color foregroundColor;
            public static Color ForegroundColor
            {
                get => foregroundColor;
                set
                {
                    if (foregroundColor == value) return;
                    foregroundColor = value;
                    SetForegroundColor();
                }
            }

            static bool isTransparent;
            public static bool IsTransparent
            {
                get => isTransparent;
                set
                {
                    if (isTransparent == value) return;
                    isTransparent = value;
                    SetTransparency();
                }
            }

            static bool isVisible;
            public static bool IsVisible
            {
                get => isVisible;
                set
                {
                    if (isVisible == value) return;
                    isVisible = value;
                    SetVisibility();
                }
            }

            static Task SetBackgroundColor(OnError errorAction = OnError.Ignore)
            {
                try { return Thread.UI.Run(() => DoSetBackgroundColor()); }
                catch (Exception ex)
                {
                    return errorAction.Apply(ex, "Failed to set the StatusBar background color.");
                }
            }

            static Task SetForegroundColor(OnError errorAction = OnError.Ignore)
            {
                try { return Thread.UI.Run(() => DoSetForegroundColor()); }
                catch (Exception ex)
                {
                    return errorAction.Apply(ex, "Failed to set the StatusBar foreground color.");
                }
            }

            static Task SetTransparency(OnError errorAction = OnError.Ignore)
            {
                try { return Thread.UI.Run(() => DoSetTransparency()); }
                catch (Exception ex)
                {
                    return errorAction.Apply(ex, "Failed to set the StatusBar foreground color.");
                }
            }

            static Task SetVisibility(OnError errorAction = OnError.Ignore)
            {
                try { return Thread.UI.Run(() => DoSetVisibility()); }
                catch (Exception ex)
                {
                    return errorAction.Apply(ex, "Failed to set the StatusBar foreground color.");
                }
            }
        }

        internal static bool darkMode;

        public static bool DarkMode { get => darkMode; set => darkMode = value; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static float Width { get; private set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static float Height { get; private set; }

        static Func<float> WidthProvider, HeightProvider;

        public static readonly AsyncEvent OrientationChanged = new AsyncEvent(ConcurrentEventRaisePolicy.Queue);

        public static Size GetSize() => new Size(Width, Height);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void ConfigureSize(Func<float> widthProvider, Func<float> heightProvider)
        {
            WidthProvider = widthProvider;
            HeightProvider = heightProvider;

#if UWP
            if (UIRuntime.IsDevMode && UIRuntime.Inspector.IsRotating)
            {
                Width = widthProvider();
                Height = heightProvider();
                return;
            }
#endif
            OrientationChanged.HandleOnUI(UpdateLayout);
            App.CameToForeground += () => Thread.UI.Run(() => UpdateLayout());

            UpdateLayout();
        }

        internal static void UpdateLayout()
        {
            var newWidth = WidthProvider();
            var newHeight = HeightProvider();
            if (Width.AlmostEquals(newWidth) && Height.AlmostEquals(newHeight)) return;

            Width = newWidth;
            Height = newHeight;

#if UWP
            var extra = UIRuntime.IsDevMode ? UIRuntime.Inspector.CurrentWidth : 0;
            UIRuntime.RenderRoot.Size(Width + extra, Height);
#endif

            if (View.Root?.IsRendered() != true) return;

            View.Root.Size(Width, Height);
            Nav.DisposeCache();
            View.Root.ApplyCssToBranch().RunInParallel();
        }

        public static async Task<FileInfo> SaveAsImage(View input, OnError errorAction = OnError.Alert)
        {
            try { return await Thread.UI.Run(() => DoSaveAsImage(input.Native)); }
            catch (Exception ex)
            {
                await errorAction.Apply(ex, "Failed to save a view as image.");
                return null;
            }
        }

        public static async Task<FileInfo> SaveAsImage(object input, OnError errorAction = OnError.Alert)
        {
            try { return await Thread.UI.Run(() => DoSaveAsImage(input)); }
            catch (Exception ex)
            {
                await errorAction.Apply(ex, "Failed to save a view as image.");
                return null;
            }
        }
    }
}
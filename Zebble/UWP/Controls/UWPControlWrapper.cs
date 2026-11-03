namespace Zebble.UWP
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;
    using Windows.UI.Xaml.Media;
    using controls = Windows.UI.Xaml.Controls;
    using xaml = Windows.UI.Xaml;

    class UWPControlWrapper
    {
        WeakReference<View> ViewRef;
        View View => ViewRef.GetTargetOrDefault();

        internal controls.Border Native;
        controls.Grid BackgroundImageLayer;
        ImageView BackgroundImage;

        public UWPControlWrapper(Renderer renderer)
        {
            ViewRef = renderer.View.GetWeakReference();
            Native = new controls.Border { Child = renderer.NativeElement };
            HandleEvents();
            BorderChanged();
            BorderRadiusChanged();
        }

        public Task<UWPControlWrapper> Render()
        {
            BackgroundChanged(UIChangedEventArgs.Empty);
            return Task.FromResult(this);
        }

        public override string ToString() => View?.GetFullPath();

        void HandleEvents()
        {
            View.BackgroundImageChanged.HandleOnUI(BackgroundChanged);
            View.BorderChanged.HandleOnUI(BorderChanged);
            View.BorderRadiusChanged.HandleOnUI(BorderRadiusChanged);
            View.BackgroundImageParametersChanged.HandleOnUI(BackgroundImageParametersChanged);
        }

        void BackgroundImageParametersChanged()
        {
            if (BackgroundImageLayer != null)
            {
                var child = BackgroundImageLayer.Children.OfType<controls.Border>().FirstOrDefault()?.Child;
                if (child is controls.Border actualBackImage)
                    if (actualBackImage.Background is ImageBrush oldBrush)
                    {
                        oldBrush.AlignmentX = View.BackgroundImageAlignment.RenderX();
                        oldBrush.AlignmentY = View.BackgroundImageAlignment.RenderY();
                        oldBrush.Stretch = View.BackgroundImageStretch.Render();
                        actualBackImage.Background = null;
                        actualBackImage.Background = oldBrush;
                    }
                    else
                        BackgroundChanged(null); //This execute just for first time.
            }
        }

        void BackgroundChanged() => BackgroundChanged(UIChangedEventArgs.Empty);

        public void BackgroundChanged(UIChangedEventArgs args)
        {
            if (View is null) return;

            var color = View.BackgroundColor;
            if (args is UIChangedEventArgs<Color> c) color = c.Value;

            if (args.Animated())
            {
                Color oldColor()
                {
                    return (Native.Background as SolidColorBrush)
                        .Get(x => new Color(x.Color.R, x.Color.G, x.Color.B)) ?? Colors.Transparent;
                }

                Native.Animate(args.Animation, "(Control.Background).(SolidColorBrush.Color)", oldColor, () => color);
            }
            else
            {
                Native.Background = color.RenderBrush();
            }

            if (View is ImageView) return;
            else if (!View.Effective.HasBackgroundImage())
            {
                BackgroundImage?.Ignored();
            }
            else if (BackgroundImage is null)
            {
                CreateBackgroundImage();
            }
            else
            {
                BackgroundImage.Path(View.BackgroundImagePath)
                    .Stretch(View.BackgroundImageStretch)
                    .Alignment(View.BackgroundImageAlignment);
            }
        }

        void CreateBackgroundImage()
        {
            BackgroundImageLayer = new controls.Grid();
            xaml.FrameworkElement nativeImage = null;

            BackgroundImage = new ImageView
            {
                Path = View.BackgroundImagePath,
                Stretch = View.BackgroundImageStretch,
                Alignment = View.BackgroundImageAlignment
            }.Set(x => x.Width.BindTo(View.Width, v => v))
              .Set(x => x.Height.BindTo(View.Height, v => v));

            Thread.Pool.RunAction(async () =>
            {
                await BackgroundImage.OnPreRender();
                await UIRuntime.Render(BackgroundImage);
                nativeImage = BackgroundImage.Native();

                Thread.UI.Post(() =>
                {
                    BackgroundImageLayer.Children.Add(nativeImage);

                    if (Native.Child is xaml.FrameworkElement actualChild && actualChild != BackgroundImageLayer)
                    {
                        // Move the child to the background
                        Native.Child = BackgroundImageLayer;
                        BackgroundImageLayer.Children.Add(actualChild);
                    }
                });
            });
        }

        public void BorderChanged()
        {
            if (View is null) return;

            Native.BorderBrush = View.Effective.BorderColor().RenderBrush();
            Native.BorderThickness = View.Border.RenderThickness();
        }

        public void BorderRadiusChanged()
        {
            if (View is null) return;

            Native.CornerRadius = View.Effective.RenderCornerRadius();
        }

        public void Dispose()
        {
            var view = View;
            if (view != null)
            {
                view.BackgroundImageChanged.RemoveActionHandler(BackgroundChanged);
                view.BorderChanged.RemoveActionHandler(BorderChanged);
                view.BorderRadiusChanged.RemoveActionHandler(BorderRadiusChanged);
                view.BackgroundImageParametersChanged.RemoveActionHandler(BackgroundImageParametersChanged);
            }

            ViewRef?.SetTarget(null);

            (Native.Parent as controls.Canvas).Perform(p => p.Children.Remove(Native));
            (Native.Child as IDisposable)?.Dispose(); 

            BackgroundImage?.Dispose(); 
            BackgroundImage = null;
			
			GC.SuppressFinalize(this);
        }
    }
}
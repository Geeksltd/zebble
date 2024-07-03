namespace Zebble
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using UWP;
    using Windows.UI.Xaml.Media;
    using controls = Windows.UI.Xaml.Controls;
    using xaml = Windows.UI.Xaml;
    using Olive;

    partial class Renderer
    {
        internal xaml.FrameworkElement NativeElement;
        UWPControlWrapper ResultWrapper;
        UWPGestureRecognizer GestureRecognizer;
        readonly object RotationSyncLock = new();

        xaml.FrameworkElement NativeResult => ResultWrapper?.Native ?? NativeElement;
        controls.UIElementCollection NativeContainer;

        public async Task<xaml.FrameworkElement> Render()
        {
            if (IsDisposing) return null;

            try { await CreateNativeElement(); }
            catch (Exception ex)
            {
                throw new RenderException("Failed to create native object for " + View, ex);
            }

            ResultWrapper = await new UWPControlWrapper(this).Render();
            View.Native = NativeResult;

            if (UIRuntime.IsDebuggerAttached)
            {
                ResultWrapper?.Native.Perform(x => x.Name = "Border: " + View);
                NativeElement.Name = View.ToStringOrEmpty();
            }

            NativeElement.MinHeight = NativeElement.MinWidth = 0;
            NativeElement.MaxHeight = NativeElement.MaxWidth = double.MaxValue;

            NativeResult.Loaded += NativeResult_Loaded;

            if (View.HandlesGestures())
                GestureRecognizer = new UWPGestureRecognizer(NativeResult, View);

            if (!View.IsEffectivelyVisible()) NativeResult.Visibility = xaml.Visibility.Collapsed;
            if (View.Opacity != 1) OnOpacityChanged(new UIChangedEventArgs<float>(View, View.Opacity));

            var transform = new TransformationChangedEventArgs(View);
            if (transform.HasValue()) Transform(transform);

            return NativeResult;
        }

        async Task CreateNativeElement()
        {
            if (View is IRenderedBy) NativeElement = CreateFromNativeRenderer();
            else if (View is TextView tv) NativeElement = new UWPTextBlock(tv);
            else if (View is TextInput input)
            {
                if (input.GetEffectiveTextMode() == TextMode.Password)
                    NativeElement = await (RenderOrchestrator = new UWPPasswordBox(this)).Render();
                else
                    NativeElement = await (RenderOrchestrator = new UWPTextBox(this)).Render();
            }
            else if (View is ImageView image)
                NativeElement = await (RenderOrchestrator = new UWPImageView(image)).Render();
            else if (View is ScrollView scroll)
                NativeElement = await (RenderOrchestrator = new UWPScrollView(scroll)).Render();
            else if (View is BlurBox blurBox) NativeElement = new UWPBlurBox(this, blurBox);
            else NativeElement = new UWPCanvas(this, View);
        }

        internal void Apply(string property, UIChangedEventArgs change)
        {
            switch (property)
            {
                case "[ADD]":
                    AddToNativeParent();
                    break;
                case "[REMOVE]":
                    DoRemove();
                    break;
                case "[DISPOSE]":
                    View?.Dispose();
                    break;
                case "Transform":
                    Transform((TransformationChangedEventArgs)change);
                    break;
                case "Bounds":
                    OnBoundsChanged((BoundsChangedEventArgs)change);
                    break;
                case "Opacity":
                    OnOpacityChanged((UIChangedEventArgs<float>)change);
                    break;
                case "ZIndex":
                    ZIndexChanged();
                    break;
                case "Visibility":
                    OnVisibilityChanged();
                    break;
                case "BackgroundColor":
                    ResultWrapper?.BackgroundChanged(change);
                    break;
            }

            (NativeResult as UIChangeCommand.IHandler)?.Apply(property, change);
            (NativeElement as UIChangeCommand.IHandler)?.Apply(property, change);
        }

        void NativeResult_Loaded(object _, xaml.RoutedEventArgs __)
        {
            NativeResult.Perform(x => x.Loaded -= NativeResult_Loaded);
            if (!IsDisposing) OnLoaded();
        }

        async void Transform(TransformationChangedEventArgs args)
        {
            if (IsDead(out var view)) return;

            var projection = (PlaneProjection)(NativeResult.Projection ?? (NativeResult.Projection = new PlaneProjection()));
            var transform = (ScaleTransform)(NativeResult.RenderTransform as ScaleTransform ?? (NativeResult.RenderTransform = new ScaleTransform()));

            var toBe = new
            {
                Transform = args.RenderTransform(),
                Projection = args.RenderProjection()
            };

            projection.CenterOfRotationX = toBe.Projection.CenterOfRotationX;
            projection.CenterOfRotationY = toBe.Projection.CenterOfRotationY;

            if (!args.Animated())
            {
                NativeResult.Projection = toBe.Projection;
                NativeResult.RenderTransform = toBe.Transform;
                return;
            }
            if (projection.RotationX != toBe.Projection.RotationX)
                NativeResult.Animate(args.Animation, "(UIElement.Projection).(PlaneProjection.RotationX)",
                () => projection.RotationX, () => toBe.Projection.RotationX);

            if (projection.RotationY != toBe.Projection.RotationY)
                NativeResult.Animate(args.Animation, "(UIElement.Projection).(PlaneProjection.RotationY)",
                () => projection.RotationY, () => toBe.Projection.RotationY);

            if (projection.RotationZ != toBe.Projection.RotationZ)
                NativeResult.Animate(args.Animation, "(UIElement.Projection).(PlaneProjection.RotationZ)",
                () => projection.RotationZ, () => toBe.Projection.RotationZ);

            NativeResult.AnimateTransform(transform, args.Animation,
                    "(ScaleTransform.ScaleX)", () => transform.ScaleX, () => toBe.Transform.ScaleX);

            NativeResult.AnimateTransform(transform, args.Animation,
                "(ScaleTransform.ScaleY)", () => transform.ScaleY, () => toBe.Transform.ScaleY);

            //await Task.Delay(args.Animation.Duration.Subtract(10.Milliseconds()));

            // Why did we set rotation-related props, but didn't set scale-related ones?
            //projection.RotationZ = toBe.Projection.RotationZ;
            //projection.RotationX = toBe.Projection.RotationX;
            //projection.RotationY = toBe.Projection.RotationY;
        }

        void ZIndexChanged()
        {
            if (!IsDead(out var view))
                controls.Canvas.SetZIndex(NativeResult, view.ZIndex);
        }

        void OnLoaded()
        {
            if (IsDisposing) return;

            if (View.Panning.IsHandled() || View.Swiped.IsHandled())
            {
                foreach (var item in View.GetAllParents().OfType<ScrollView>())
                    UWPScrollView.EnableManual(item);
            }

            View.RaiseShown();
        }

        void OnVisibilityChanged()
        {
            if (IsDisposing) return;
            NativeResult.Visibility = View.IsEffectivelyVisible() ? xaml.Visibility.Visible : xaml.Visibility.Collapsed;
        }

        void OnOpacityChanged(UIChangedEventArgs<float> args)
        {
            if (IsDead(out var view)) return;
            if (args.Animated())
            {
                NativeResult.Animate(args.Animation, "Opacity", () => NativeResult.Opacity, () => args.Value);
            }
            else
            {
                NativeResult.Opacity = args.Value;
            }
        }

        void OnBoundsChanged(BoundsChangedEventArgs args)
        {
            if (!IsDead(out var view))
            {
                view.SetPosition(NativeResult, args);
                view.SetSize(NativeElement, args);
            }
        }

        void AddToNativeParent()
        {
            var view = View;
            var parent = view?.parent;
            var nativeParent = parent?.Native() as xaml.UIElement;

            if (nativeParent is controls.Border wrapper) nativeParent = wrapper.Child;

            var nativeChild = NativeResult;

            // High concurrency. Already disposed:
            if (IsDisposing || view == null || view.IsDisposing) return;
            if (parent == null || parent.IsDisposed || nativeParent == null || nativeChild is null) return;

            if (IsDisposing) return;

            nativeChild.Visibility = View.IsEffectivelyVisible() ? xaml.Visibility.Visible : xaml.Visibility.Collapsed;

            if (nativeChild.Parent != null)
            {
                if (nativeChild.Parent != nativeParent)
                    Log.For(this).Error($"The native view already has a parent: {view.GetFullPath()}");

                return;
            }

            try
            {
                if (nativeParent is controls.Panel panel) NativeContainer = panel.Children;
                else if (nativeParent is controls.ScrollViewer scroller) NativeContainer = (scroller.Content as controls.Panel)?.Children;
                else throw new RenderException($"{nativeParent.GetType().Name} is not a supported container for rendering.");

                if (NativeContainer?.None(x => x == nativeChild) == true)
                    NativeContainer?.Add(nativeChild);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                /*No logging is needed. Is this a strange random UWP bug ?*/
            }

            var size = view.GetNativeSize(NativeElement);
            NativeElement.Width = size.Width;
            NativeElement.Height = size.Height;
            (view as Canvas)?.ApplyClip(NativeElement);
            controls.Canvas.SetLeft(nativeChild, view.ActualX);
            controls.Canvas.SetTop(nativeChild, view.ActualY);
            controls.Canvas.SetZIndex(nativeChild, view.ZIndex);
        }

        void DoRemove()
        {
            if (!IsDisposing) NativeResult?.RemoveFromSuperview(); // NativeContainer?.Remove(NativeResult);
        }

        public void Dispose()
        {
            IsDisposing = true;

            NativeResult.Perform(x => x.Loaded -= NativeResult_Loaded);

            ResultWrapper?.Dispose();
            RenderOrchestrator?.Dispose();
            RenderOrchestrator = null;

            GestureRecognizer?.Dispose();
            GestureRecognizer = null;
            ResultWrapper = null;
            NativeContainer = null;
            View = null;

            (NativeElement as IDisposable)?.Dispose();
            NativeElement = null;
			
			GC.SuppressFinalize(this);
        }
    }
}
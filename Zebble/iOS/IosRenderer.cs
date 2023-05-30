namespace Zebble
{
    using CoreAnimation;
    using CoreGraphics;
    using IOS;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using UIKit;
    using Olive;

    partial class Renderer
    {
        UIView Result;
        CALayer Layer => Result.Layer;

        public async Task<UIView> Render()
        {
            if (IsDead(out var view)) return null;

            using (await view.DomLock.Lock())
            {
                Result = null;

                if (view is IRenderedBy) Result = CreateFromNativeRenderer();
                else if (view is TextView tv) Result = new IosLabel(tv);
                else if (view is TextInput ti)
                {
                    if (ti.Lines > 1) Result = new IosTextAreaWrapper(ti);
                    else Result = new IosTextBox(ti);
                }
                else if (view is ImageView img) Result = await new IosImageWrapper(img).Render();
                else
                {
                    if (view is ScrollView sc) Result = await new IosScrollView(sc).GetResult();
                    else Result = new IosContainer(view);

                    Result.ResignFirstResponder();
                }

                if (view is Canvas canvas) Result.ClipsToBounds = canvas.ClipChildren;

                Result.AccessibilityIdentifier = view.id;

                ConfigureCommonSettings();
                HandleEvents();
                AddGestures();

                if (view.Tapped.IsHandled() || view.Panning.IsHandled() || view.Swiped.IsHandled() || view.PanFinished.IsHandled()
                    || view.LongPressed.IsHandled())
                    Result.UserInteractionEnabled = true;

                return Result;
            }
        }

        void ConfigureCommonSettings()
        {
            if (IsDead(out var view)) return;

            // This is the default value.
            Layer.AnchorPoint = new CGPoint(view.TransformOriginX, view.TransformOriginY);
            Result.Frame = view.GetFrame();

            if (!view.IsEffectivelyVisible()) Result.Hidden = true;
            Result.Opaque = true;

            if (view.Opacity != 1) Result.Alpha = view.Opacity;

            RenderBorder();
            if (view is ImageView) Layer.MasksToBounds = true;

            SetBackgroundColor(new UIChangedEventArgs<Color>(View, View.BackgroundColor) { Animation = null });
            SetBackgroundImage();

            Layer.RasterizationScale = UIScreen.MainScreen.Scale;
            var transform = new TransformationChangedEventArgs(view);

            if (transform.HasValue()) Transform(transform);
        }

        internal void Apply(string property, UIChangedEventArgs change)
        {
            switch (property)
            {
                case "[ADD]":
                    AddToNativeParent();
                    break;
                case "[REMOVE]":
                    RemoveFromNativeParent();
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
                    OnZIndexChanged();
                    break;
                case "Visibility":
                    OnVisibilityChanged();
                    break;
                case "BackgroundColor":
                    SetBackgroundColor((UIChangedEventArgs<Color>)change);
                    break;
                case "ClipChildren":
                    Result.ClipsToBounds = ((UIChangedEventArgs<bool>)change).Value;
                    break;
                case "TextColor":
                    break;
                default:
                    Log.For(this).Error($"The {property} is not handled to be animated.");
                    return;
            }

            (Result as UIChangeCommand.IHandler)?.Apply(property, change);
        }

        void HandleEvents()
        {
            if (IsDead(out View view)) return;

            view.BackgroundImageChanged.HandleOnUI(SetBackgroundImage);
            view.BorderChanged.HandleOnUI(RenderBorder);
            view.BorderRadiusChanged.HandleOnUI(RenderBorder);
        }

        void OnBoundsChanged(BoundsChangedEventArgs args)
        {
            if (IsDead(out var view)) return;

            // https://developer.apple.com/documentation/quartzcore/calayer/1410791-position
            var relativeX = (args.Width * Layer.AnchorPoint.X) + args.X;
            var relativeY = (args.Height * Layer.AnchorPoint.Y) + args.Y;
            var newPosition = new CGPoint(relativeX, relativeY);

            if (Layer.Position.ToString() != newPosition.ToString())
            {
                var xChanged = !Layer.Position.X.AlmostEquals(newPosition.X);
                var yChanged = !Layer.Position.Y.AlmostEquals(newPosition.Y);

                if (xChanged || yChanged)
                {
                    if (args.Animated())
                    {
                        if (xChanged) args.Animation.AddNative(Layer, "position.x", newPosition.X.ToNs());
                        if (yChanged) args.Animation.AddNative(Layer, "position.y", newPosition.Y.ToNs());
                    }
                    else
                    {
                        // Keep setting position value even when
                        // the change should be applied through animations
                        Layer.Position = newPosition;
                    }
                }
            }

            // X and Y of the bounds are always 0
            // https://stackoverflow.com/questions/1210047/cocoa-whats-the-difference-between-the-frame-and-the-bounds#comment1033809_1210141
            var newSize = new CGSize(args.Width, args.Height);
            if (Layer.Bounds.Size.ToString() != newSize.ToString())
            {
                var widthChanged = !Layer.Bounds.Size.Width.AlmostEquals(newSize.Width);
                var heightChanged = !Layer.Bounds.Size.Height.AlmostEquals(newSize.Height);

                if (widthChanged || heightChanged)
                {
                    if (args.Animated())
                    {
                        if (widthChanged) args.Animation.AddNative(Layer, "bounds.size.width", newSize.Width.ToNs());
                        if (heightChanged) args.Animation.AddNative(Layer, "bounds.size.height", newSize.Height.ToNs());
                    }
                    else
                    {
                        // Keep setting position value even when
                        // the change should be applied through animations
                        Layer.Bounds = new CGRect(Layer.Bounds.Location, newSize);
                    }
                }
            }

            // TODO: As I have no idea why this was needed, this has been commented out
            // The second reason is because it was setting the Frame prop which we'll consider as a bad practice from now on
            // As gradient color is a sublayer, sync its size.
            // Layer.SyncBackgroundFrame(Result.Frame);

            RenderBorder();
        }

        void OnOpacityChanged(UIChangedEventArgs<float> args)
        {
            if (IsDead(out var view)) return;

            if (args.Animated())
                args.Animation.AddNative(Layer, "opacity", args.Value.ToNs());
            Layer.Opacity = args.Value;
        }

        void Transform(TransformationChangedEventArgs args)
        {
            if (IsDead(out var view)) return;

            Layer.AnchorPoint = new CGPoint(args.OriginX, args.OriginY);

            if (args.Animated())
            {
                args.Animation.AddNative(Layer, "transform.rotation.x", args.RotateX.ToRadians().ToNs());
                args.Animation.AddNative(Layer, "transform.rotation.y", args.RotateY.ToRadians().ToNs());
                args.Animation.AddNative(Layer, "transform.rotation.z", args.RotateZ.ToRadians().ToNs());

                args.Animation.AddNative(Layer, "transform.scale.x", args.ScaleX.ToNs());
                args.Animation.AddNative(Layer, "transform.scale.y", args.ScaleY.ToNs());
            }
            Layer.Transform = args.Render();

            RenderBorder();
        }

        void OnVisibilityChanged()
        {
            if (IsDead(out var view)) return;
            Result.Hidden = !view.IsEffectivelyVisible();
        }

        void UpdateZOrder()
        {
            if (IsDead(out var view)) return;

            var withSiblings = view.parent?.AllChildren.ToArray();
            if (withSiblings == null) return;

            var parent = view.parent;
            var nativeParent = parent?.Native();
            if (nativeParent == null) return;

            if (nativeParent.Subviews.Length <= 1)
                return; // no need for reposition if it has less than two children

            var correctOrder =
                withSiblings.OrderBy(x => x.ZIndex).ThenBy(x => withSiblings.IndexOf(x)).Select(c => c.Native()).ExceptNull()
                .Where(x => nativeParent.Subviews.Contains(x)).ToArray();

            if (!nativeParent.Subviews.IsEquivalentTo(correctOrder))
                Log.For(this).Error("Native parent's children are modified while setting the ZOrder");
            //throw new ArgumentOutOfRangeException("this should not happen.");

            foreach (var nView in correctOrder)
                nativeParent.BringSubviewToFront(nView);
        }

        void AddToNativeParent()
        {
            if (IsDead(out var view)) return;

            var parent = view.parent;
            var nativeParent = parent?.Native();

            // High concurrency. Already disposed:
            if (view.IsDisposing || parent == null || parent.IsDisposed || nativeParent is null) return;

            Result.Hidden = true;
            OnBoundsChanged(new BoundsChangedEventArgs(view) { Animation = null });

            nativeParent.AddSubview(Result);
            UpdateZOrder();

            nativeParent.ReloadInputViews();
            OnVisibilityChanged();

            view.RaiseShown();
        }

        void OnZIndexChanged()
        {
            if (IsDead(out var view)) return;

            UpdateZOrder();
        }

        public static void Redraw(View view) => view.Parent?.Native()?.SetNeedsDisplay();

        void RemoveFromNativeParent()
        {
            if (IsDead(out View view)) return;

            try
            {
                var superView = Result.Superview;
                Result.RemoveFromSuperview();
                superView?.ReloadInputViews();
            }
            catch (Exception ex)
            {
                Log.For(this).Error(ex, "Error removing object " + Result);
            }

            UpdateZOrder();
        }

        public void Dispose()
        {
            IsDisposing = true;
            RenderOrchestrator?.Dispose(); RenderOrchestrator = null;
            Result?.Dispose();
            BackgroundImage?.Dispose();
            Result = null;
            BackgroundImage = null;
            View = null;
        }
    }
}
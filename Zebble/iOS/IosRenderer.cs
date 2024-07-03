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
        CALayer Layer => Result?.Layer;

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
                    else if (view is BlurBox blurBox) Result = new IosBlurBox(blurBox);
                    else Result = new IosContainer(view);

                    Result.ResignFirstResponder();
                }

                if (view is Canvas canvas) Result.ClipsToBounds = canvas.ClipChildren;

                Result.AccessibilityIdentifier = view.id;

                ConfigureCommonSettings();
                HandleEvents();

                if (view.HandlesGestures()) AddGestures();

                if (view.Tapped.IsHandled() || view.Panning.IsHandled() || view.Swiped.IsHandled() || view.PanFinished.IsHandled()
                    || view.LongPressed.IsHandled())
                    Result.UserInteractionEnabled = true;

                return Result;
            }
        }

        void ConfigureCommonSettings()
        {
            if (IsDead(out var view)) return;

            var layer = Layer;
            if (layer is not null)
            {
                // This is the default value.
                Layer.AnchorPoint = new CGPoint(view.TransformOriginX, view.TransformOriginY);
            }

            Result.Frame = view.GetFrame();

            if (!view.IsEffectivelyVisible()) Result.Hidden = true;
            Result.Opaque = true;

            if (view.Opacity != 1) Result.Alpha = view.Opacity;

            RenderBorder();
            if (view is ImageView) layer.Set(x => x.MasksToBounds = true);

            SetBackgroundColor(new UIChangedEventArgs<Color>(View, View.BackgroundColor) { Animation = null });
            SetBackgroundImage();

            layer.Set(x => x.RasterizationScale = UIScreen.MainScreen.Scale);
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
                    Result.Set(x => x.ClipsToBounds = ((UIChangedEventArgs<bool>)change).Value);
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

            var layer = Layer;
            if (layer is null) return;

            // https://developer.apple.com/documentation/quartzcore/calayer/1410791-position
            var relativeX = (args.Width * layer.AnchorPoint.X) + args.X;
            var relativeY = (args.Height * layer.AnchorPoint.Y) + args.Y;
            var newPosition = new CGPoint(relativeX, relativeY);

            if (layer.Position.ToString() != newPosition.ToString())
            {
                var xChanged = !layer.Position.X.AlmostEquals(newPosition.X);
                var yChanged = !layer.Position.Y.AlmostEquals(newPosition.Y);

                if (xChanged || yChanged)
                {
                    if (args.Animated())
                    {
                        if (xChanged) args.Animation.AddNative(layer, "position.x", newPosition.X.ToNs());
                        if (yChanged) args.Animation.AddNative(layer, "position.y", newPosition.Y.ToNs());
                    }
                    else
                    {
                        // Keep setting position value even when
                        // the change should be applied through animations
                        layer.Position = newPosition;
                    }
                }
            }

            // X and Y of the bounds are always 0
            // https://stackoverflow.com/questions/1210047/cocoa-whats-the-difference-between-the-frame-and-the-bounds#comment1033809_1210141
            var newSize = new CGSize(args.Width, args.Height);
            if (layer.Bounds.Size.ToString() != newSize.ToString())
            {
                var widthChanged = !layer.Bounds.Size.Width.AlmostEquals(newSize.Width);
                var heightChanged = !layer.Bounds.Size.Height.AlmostEquals(newSize.Height);

                if (widthChanged || heightChanged)
                {
                    if (args.Animated())
                    {
                        if (widthChanged) args.Animation.AddNative(layer, "bounds.size.width", newSize.Width.ToNs());
                        if (heightChanged) args.Animation.AddNative(layer, "bounds.size.height", newSize.Height.ToNs());
                    }
                    else
                    {
                        // Keep setting position value even when
                        // the change should be applied through animations
                        layer.Bounds = new CGRect(layer.Bounds.Location, newSize);
                    }
                }
            }

            // TODO: As I have no idea why this was needed, this has been commented out
            // The second reason is because it was setting the Frame prop which we'll consider as a bad practice from now on
            // As gradient color is a sublayer, sync its size.
            // Layer.SyncBackgroundFrame(Result.Frame);

            (Result as IosBlurBox)?.OnBoundsChanged();

            RenderBorder();
        }

        void OnOpacityChanged(UIChangedEventArgs<float> args)
        {
            if (IsDead(out var _)) return;

            var layer = Layer;
            if (layer is null) return;

            if (args.Animated())
                args.Animation.AddNative(layer, "opacity", args.Value.ToNs());
            layer.Opacity = args.Value;
        }

        void Transform(TransformationChangedEventArgs args)
        {
            if (IsDead(out var _)) return;

            var layer = Layer;
            if (layer is null) return;

            layer.AnchorPoint = new CGPoint(args.OriginX, args.OriginY);

            if (args.Animated())
            {
                args.Animation.AddNative(layer, "transform.rotation.x", args.RotateX.ToRadians().ToNs());
                args.Animation.AddNative(layer, "transform.rotation.y", args.RotateY.ToRadians().ToNs());
                args.Animation.AddNative(layer, "transform.rotation.z", args.RotateZ.ToRadians().ToNs());

                args.Animation.AddNative(layer, "transform.scale.x", args.ScaleX.ToNs());
                args.Animation.AddNative(layer, "transform.scale.y", args.ScaleY.ToNs());
            }

            layer.Transform = args.Render();

            RenderBorder();
        }

        void OnVisibilityChanged()
        {
            if (IsDead(out var view)) return;
            Result.Set(x => x.Hidden = !view.IsEffectivelyVisible());
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

            foreach (var nView in correctOrder)
                nativeParent.BringSubviewToFront(nView);
        }

        void AddToNativeParent()
        {
            if (IsDead(out var view)) return;

            var parent = view.parent;
            var nativeParent = parent?.Native();
            var result = Result;

            // High concurrency. Already disposed:
            if (view.IsDisposing || parent == null || parent.IsDisposed || nativeParent is null || result is null) return;

            result.Hidden = true;
            OnBoundsChanged(new BoundsChangedEventArgs(view) { Animation = null });

            nativeParent.AddSubview(result);
            UpdateZOrder();

            nativeParent.ReloadInputViews();
            OnVisibilityChanged();

            view.RaiseShown();
        }

        void OnZIndexChanged()
        {
            if (IsDead(out var _)) return;

            UpdateZOrder();
        }

        public static void Redraw(View view)
            => view.Parent?.Native()?.SetNeedsDisplay();

        void RemoveFromNativeParent()
        {
            if (IsDead(out View _)) return;

            try
            {
                var superView = Result?.Superview;
                Result?.RemoveFromSuperview();
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

            RenderOrchestrator?.Dispose();
            RenderOrchestrator = null;

            BackgroundImage?.Dispose();
            BackgroundImage = null;

            Result?.Dispose();
            Result = null;

            View = null;
			
			GC.SuppressFinalize(this);
        }
    }
}
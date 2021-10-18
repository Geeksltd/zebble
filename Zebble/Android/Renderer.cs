namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using Android.Content;
    using Android.Views;
    using AndroidOS;
    using Zebble.Device;

    partial class Renderer
    {
        Android.Views.View Result;

        public static Context Context => Android.App.Application.Context;

        void AddToNativeParent()
        {
            if (IsDead(out var view)) return;

            var parent = view.parent;
            var nativeParent = parent?.Native;

            if (parent == null || parent.IsDisposed || nativeParent is null) return; // High concurrency. Already disposed

            ConfigureCommonSettings();
            Result.Visibility = ViewStates.Gone;

            // if (!view.Shown.IsHandled()) view.IsShown = true;
            // else
            Result.ViewAttachedToWindow += Result_ViewAttachedToWindow;

            if (nativeParent is IScrollView scrollview)
            {
                scrollview.GetContainer().AddView(Result);
            }
            else if (nativeParent is ViewGroup viewGroup)
            {
                if (Result.Parent is ViewGroup oldParent) oldParent.RemoveView(Result);
                viewGroup.AddView(Result);
            }

            if (view.IsEffectivelyVisible()) Result.Visibility = ViewStates.Visible;
        }

        void Result_ViewAttachedToWindow(object sender, Android.Views.View.ViewAttachedToWindowEventArgs e)
        {
            if (IsDead(out var view)) return;
            Result.ViewAttachedToWindow -= Result_ViewAttachedToWindow;

            view.RaiseShown();
        }

        public async Task<Android.Views.View> Render()
        {
            if (IsDead(out var view)) return null;
            if (IsDisposing) return null;

            Result = null;

            try { RenderResult(); }
            catch (Exception ex) { throw new Exception("Failed to render " + view, ex); }

            if (UIRuntime.IsDebuggerAttached)
                Result.Tag = new JavaWrapper(view);

            HandleEvents();

            return Result;
        }

        void RenderResult()
        {
            if (IsDead(out var view)) return;

            if (view is IRenderedBy) Result = CreateFromNativeRenderer();
            else if (view is TextView) Result = AndroidTextView.Create(view as TextView);
            else if (view is TextInput) Result = new AndroidTextInput(view as TextInput);
            else if (view is ImageView) Result = AndroidImageFactory.Create(view as ImageView);
            else if (view is ScrollView) Result = ScrollViewFactory.Render(view as ScrollView);
            else if (view == UIRuntime.RenderRoot || (view as Overlay)?.BlocksGestures == true) Result = new AndroidGestureView(view);
            else Result = new AndroidCustomContainer(view);
        }

        void ConfigureCommonSettings()
        {
            if (IsDead(out var view)) return;

            Result.SetFrame(view);

            if (OS.IsAtLeast(Android.OS.BuildVersionCodes.Lollipop))
                Result.SetZ(view.ZIndex);
            else
            {
                // Note: not supported!
            }

            Result.Alpha = view.Opacity;
            if (view is Canvas canvas && Result is ViewGroup native)
            {
                native.SetClipChildren(canvas.ClipChildren);
            }

            SetBackgroundImage();
            SetBackgroundAndBorder();

            // Android Padding must be set after rendering BackGround
            SetPadding();

            var transform = new TransformationChangedEventArgs(view);
            if (transform.HasValue()) Transform(transform);
        }

        void OnVisibilityChanged()
        {
            if (IsDead(out var view)) return;
            Result.Visibility = view.IsEffectivelyVisible() ? ViewStates.Visible : ViewStates.Gone;
        }

        void RemoveFromNativeParent()
        {
            if (Result.Parent is ViewGroup group)
                group.RemoveView(Result);
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

                    // Single side borders should render again because they are depend on the size of the view.
                    SetBackgroundAndBorder();

                    if (Result is IPaddableControl) SetPadding();
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
                case "TextColor":
                    OnTextColorChanged((TextColorChangedEventArgs)change);
                    break;
                case "BackgroundColor":
                    OnBackgroundColorChanged((UIChangedEventArgs<Color>)change);
                    break;

                case "ClipChildren":
                    (Result as ViewGroup)?.SetClipChildren(((UIChangedEventArgs<bool>)change).Value);
                    break;

                case "Placeholder":
                    (Result as AndroidTextInput).Set(x => x.Hint = ((UIChangedEventArgs<string>)change).Value);
                    break;
                default: throw new NotSupportedException();
            }

            (Result as UIChangeCommand.IHandler)?.Apply(property, change);
        }

        void HandleEvents()
        {
            if (IsDead(out var view)) return;

            view.BorderChanged.HandleOnUI(SetBackgroundAndBorder);
            view.BackgroundImageChanged.HandleOnUI(SetBackgroundImage);

            if (Result is IPaddableControl)
            {
                // Single side borders have conflict with padding, so, the padding should re-rendere depend on the size of view like the borders
                view.PaddingChanged.HandleOnUI(SetPadding);
                view.BorderChanged.HandleOnUI(SetPadding);
            }
        }

        void SetPadding()
        {
            if (IsDead(out var view)) return;
            if (!(Result is IPaddableControl)) return;

            var effective = view.Effective;

            var left = Scale.ToDevice(effective.BorderAndPaddingLeft());
            var right = Scale.ToDevice(effective.BorderAndPaddingRight());
            var bottom = Scale.ToDevice(effective.BorderAndPaddingBottom());
            var top = Scale.ToDevice(effective.BorderAndPaddingTop());

            if (view is TextControl txt)
                top -= (int)txt.Font.GetUnwantedExtraTopPadding();

            Result.SetPadding(left, top, right, bottom);
        }

        public void Dispose()
        {
            IsDisposing = true;
            RenderOrchestrator?.Dispose();
            RenderOrchestrator = null;
            Result?.Dispose();
            Result = null;
            View = null;
        }
    }
}
namespace Zebble.UWP
{
    using System;
    using controls = Windows.UI.Xaml.Controls;
    using Olive;

    public class UWPCanvas : controls.Canvas, IDisposable, UIChangeCommand.IHandler
    {
        readonly WeakReference<Renderer> RendererRef;
        readonly WeakReference<View> ViewRef;
        View View => ViewRef?.GetTargetOrDefault();

        public UWPCanvas(Renderer renderer)
        {
            RendererRef = renderer.GetWeakReference();
            ViewRef = renderer?.View.GetWeakReference();
            Configure();
        }

        protected void Configure()
        {
            HandleTouches();
            VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Top;
        }

        public void Apply(string property, UIChangedEventArgs change)
        {
            switch (property)
            {
                case "BackgroundColor":
                    HandleTouches();
                    break;
                case "Bounds":
                    View.SetSize(this, (BoundsChangedEventArgs)change);
                    break;
                case "ClipChildren":
                    (View as Canvas)?.ApplyClip(this);
                    break;
            }
        }

        void HandleTouches()
        {
            var view = View; if (view is null) return;

            if (UIRuntime.IsDevMode && view.id.IsAnyOf("ZebbleInspectorHighlightMask", "ZebbleInspectorHighlightBorder"))
            {
                Background = null;
                ManipulationMode = Windows.UI.Xaml.Input.ManipulationModes.None;
                IsTapEnabled = false;
                IsHitTestVisible = false;
            }
            else Background = view.BackgroundColor.RenderBrush();
        }

        public void Dispose()
        {
            ViewRef?.SetTarget(null);
            RendererRef?.SetTarget(null);
        }
    }
}
namespace Zebble.WinUI
{
    using System;
    using controls = Microsoft.UI.Xaml.Controls;
    using Olive;

    public abstract class WinUICanvasBase<TView> : controls.Canvas, IDisposable, UIChangeCommand.IHandler where TView : View
    {
        readonly WeakReference<Renderer> RendererRef;
        readonly WeakReference<TView> ViewRef;

        protected bool IsDisposed;
        protected TView View => ViewRef?.GetTargetOrDefault();

        public WinUICanvasBase(Renderer renderer, TView view)
        {
            RendererRef = renderer.GetWeakReference();
            ViewRef = view.GetWeakReference();
            Configure();
        }

        protected void Configure()
        {
            HandleTouches();
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Top;
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
                ManipulationMode = Microsoft.UI.Xaml.Input.ManipulationModes.None;
                IsTapEnabled = false;
                IsHitTestVisible = false;
            }
            else Background = view.BackgroundColor.RenderBrush();
        }

        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                ViewRef?.SetTarget(null);
                RendererRef?.SetTarget(null);
            }
			GC.SuppressFinalize(this);
        }
    }

    public class WinUICanvas : WinUICanvasBase<View>
    {
        public WinUICanvas(Renderer renderer, View view) : base(renderer, view) { }
    }
}
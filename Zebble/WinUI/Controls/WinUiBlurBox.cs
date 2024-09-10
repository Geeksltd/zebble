namespace Zebble.WinUI
{
    public class WinUiBlurBox : WinUiCanvasBase<BlurBox>
    {
        public WinUiBlurBox(Renderer renderer, BlurBox view) : base(renderer, view)
        {
            view.BlurredChanged.Handle(MaintainBlur);
            CreateBlur();
            MaintainBlur();
        }

        void CreateBlur() { }

        void MaintainBlur()
        {
            if (IsDead(out var view)) return;
            // TODO
            // View.BackgroundColor(view.Blurred ? Colors.Black.WithAlpha(100) : null);
        }

        bool IsDead(out BlurBox result)
        {
            result = View;
            if (result is null || result.IsDisposing) return true;
            return result.IsDisposing;
        }

        public override void Dispose()
        {
            if (!IsDisposed)
                View?.BlurredChanged.RemoveActionHandler(MaintainBlur);

            base.Dispose();
        }
    }
}
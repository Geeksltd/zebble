namespace Zebble.UWP
{
    public class UWPStack : UWPCanvasBase<Stack>
    {
        public UWPStack(Renderer renderer, Stack view) : base(renderer, view)
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

        bool IsDead(out Stack result)
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
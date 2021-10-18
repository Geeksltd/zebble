namespace Zebble
{
    public partial class Canvas : View, ILayoutView
    {
        bool clipChildren = true;

        /// <summary>
        /// Determines whether the children of this canvas should be clipped to the size of the canvas (in case they are larger).
        /// By default it's True.
        /// </summary>
        public bool ClipChildren
        {
            get => clipChildren;
            set
            {
                if (clipChildren == value) return;
                clipChildren = value;
                if (IsRendered())
                    UIWorkBatch.Publish(this, "ClipChildren", new UIChangedEventArgs<bool>(this, value));
            }
        }

        public void LayoutChildren()
        {
            Width.Update();
            Height.Update();
            Padding.Update();
            Margin.Update();
        }
    }
}
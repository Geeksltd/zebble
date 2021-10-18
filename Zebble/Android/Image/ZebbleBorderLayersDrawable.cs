namespace Zebble
{
    using System;
    using Android.Graphics.Drawables;
    using Android.Runtime;
    using Zebble.Device;

    public class ZebbleBorderLayersDrawable : LayerDrawable
    {
        enum Layers { Left, Top, Right, Bottom, Background }

        readonly int Left, Top, Right, Bottom, Width, Height;

        [Preserve]
        protected ZebbleBorderLayersDrawable(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

        public ZebbleBorderLayersDrawable(View view, Drawable[] layers) : base(layers)
        {
            Left = Scale.ToDevice(view.Effective.BorderLeft());
            Top = Scale.ToDevice(view.Effective.BorderTop());
            Right = Scale.ToDevice(view.Effective.BorderRight());
            Bottom = Scale.ToDevice(view.Effective.BorderBottom());
            Width = Scale.ToDevice(view.ActualWidth);
            Height = Scale.ToDevice(view.ActualHeight);

            SetAllSides(layers, !view.BackgroundColor.IsTransparent());
        }

        internal static ZebbleBorderLayersDrawable Create(View view, Drawable baseLayer)
        {
            var borderColor = view.Effective.BorderColor().Render();

            using (var leftDrawable = new ColorDrawable(borderColor))
            using (var topDrawable = new ColorDrawable(borderColor))
            using (var rightDrawable = new ColorDrawable(borderColor))
            using (var bottomDrawable = new ColorDrawable(borderColor))
            {
                var layers = new Drawable[] { leftDrawable, topDrawable, rightDrawable, bottomDrawable, baseLayer };
                return new ZebbleBorderLayersDrawable(view, layers);
            }
        }

        void SetInset(Layers index, int left, int top, int right, int bottom)
            => SetLayerInset((int)index, left, top, right, bottom);

        void SetAllSides(Drawable[] layers, bool hasBackground)
        {
            void remove(Layers layer)
            {
                (layers[(int)layer] as ColorDrawable).Set(v => v.Color = Android.Graphics.Color.Transparent);
                SetInset(layer, 0, Height, 0, 0);
            }

            var finalLeft = Right > 0 ? (Width - Right) : 0;
            if (finalLeft == 0) remove(Layers.Left);
            else SetInset(Layers.Left, finalLeft, 0, 0, 0);

            var finalRight = Left > 0 ? (Width - Left) : 0;
            if (finalRight == 0) remove(Layers.Right);
            else SetInset(Layers.Right, 0, 0, finalRight, 0);

            var finalTop = Bottom > 0 ? (Height - Bottom) : 0;
            if (finalTop == 0) remove(Layers.Top);
            else SetInset(Layers.Top, 0, finalTop, 0, 0);

            var finalBottom = Top > 0 ? (Height - Top) : 0;
            if (finalBottom == 0) remove(Layers.Bottom);
            else SetInset(Layers.Bottom, 0, 0, 0, finalBottom);

            if (hasBackground) SetInset(Layers.Background, Left, Top, Right, Bottom);
            else remove(Layers.Background);
        }
    }
}
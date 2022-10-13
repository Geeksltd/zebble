using System;
using System.Linq;
using System.Threading.Tasks;
using Olive;

namespace Zebble
{
    partial class View
    {
        internal void RaiseBoundsChanged()
        {
            if (IsRendered())
                UIWorkBatch.Publish(this, "Bounds", new BoundsChangedEventArgs(this));
        }

        /// <summary>
        /// This should not be required. Use it for debugging.
        /// If calling it fixes a problem, please contact the Zebble team and let us know to investigate.
        /// </summary>
        public Task TrySyncLayout()
        {
            foreach (var s in WithAllDescendants().OfType<ILayoutView>().Reverse().ToArray())
                s.LayoutChildren();

            if (!IsRendered()) return Task.CompletedTask;

            return Thread.UI.Run(() => WithAllDescendants().Where(v => v.IsRendered()).ToArray().Do(v => v.SyncNativeAttributes()));
        }

        protected virtual void SyncNativeAttributes()
        {
            // TODO: Expand this to cover other attributes if required.
            Renderer?.Apply("Bounds", new BoundsChangedEventArgs(this));
            Renderer?.Apply("Opacity", new UIChangedEventArgs<float>(this, Opacity));
            Renderer?.Apply("Visibility", new UIChangedEventArgs(this));
        }
    }

    public class UIChangedEventArgs
    {
        internal static readonly UIChangedEventArgs Empty = new();
        public Animation Animation;
        protected View View;

        public UIChangedEventArgs(View view)
        {
            Animation = view.AnimationContext;
            if (UIRuntime.IsDevMode)
                if (System.Diagnostics.Debugger.IsAttached) View = view;
        }

        protected UIChangedEventArgs() { }

        internal bool Animated() => Animation?.IsStarted == false;
    }

    public class UIChangedEventArgs<TValue> : UIChangedEventArgs
    {
        public TValue Value;

        internal static readonly UIChangedEventArgs<TValue> Empty = new();

        public UIChangedEventArgs(View view, TValue value) : base(view) => Value = value;

        UIChangedEventArgs() : base() { }

        public override string ToString() => GetType().Name.TrimEnd("ChangedEventArgs") + $"-> {Value} ||| {View}";
    }

    public class TextColorChangedEventArgs : UIChangedEventArgs<Color>
    {
        public TextColorChangedEventArgs(View view, Color value) : base(view, value) { }
    }

    partial class TransformationChangedEventArgs : UIChangedEventArgs
    {
        public TransformationChangedEventArgs(View view) : base(view)
        {
            View = view;

            OriginX = view.TransformOriginX;
            OriginY = view.TransformOriginY;

            ScaleX = view.ScaleX;
            ScaleY = view.ScaleY;
            RotateX = view.RotationX;
            RotateY = view.RotationY;
            RotateZ = view.Rotation;
        }

        public float OriginX, OriginY, RotateZ, RotateX, RotateY, ScaleX, ScaleY;

        public float AbsoluteOriginX() => OriginX * View.ActualWidth;

        public float AbsoluteOriginY() => OriginY * View.ActualHeight;

        public override string ToString()
        {
            return $"Rotate (X:{RotateX}, Y:{RotateY}, Z:{RotateZ}) Scale (X:{ScaleX}, Y:{ScaleY})" +
             ((OriginX != 0.5f || OriginX != .5f) ? $" Around ({OriginX}, {OriginY})" : "");
        }

        public bool HasValue()
        {
            return ScaleX != 1 || ScaleY != 1 || RotateX != 0 || RotateY != 0 || RotateZ != 0 || OriginX != 0.5 || OriginY != 0.5;
        }
    }

    [EscapeGCop("X and Y are acceptable here.")]
    class BoundsChangedEventArgs : UIChangedEventArgs
    {
        public float Width, Height, X, Y;

        public BoundsChangedEventArgs(View view) : base(view)
        {
            Width = view.ActualWidth;
            Height = view.ActualHeight;
            X = view.ActualX;
            Y = view.ActualY;
        }

        public override string ToString() => $"W: {Width}, H: {Height}, X:{X}, Y:{Y} ||| {View}";
    }
}
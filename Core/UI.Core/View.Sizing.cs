namespace Zebble
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using Olive;

    partial class View
    {
        const float DEFAULT_EMPTY_VIEW_HEIGHT = 30;

        internal readonly AsyncEvent PaddingChanged = new AsyncEvent();
        public readonly Gap Padding, Margin;
        public readonly Length Width, Height;

        [EscapeGCop("X and Y names are meaningful here.")]
        public readonly Length X, Y;

        /// <summary>
        /// Returns the calculated height plus the total vertical margin.
        /// </summary>
        public float CalculateTotalHeight() => ActualHeight + Margin.Vertical();

        /// <summary>
        /// Returns the calculated width plus the total horizontal margin.
        /// </summary>
        public float CalculateTotalWidth() => ActualWidth + Math.Max(0, Margin.Left()) + Math.Max(0, Margin.Right());

        public float ActualBottom => ActualY + ActualHeight;

        public float ActualRight => ActualX + ActualWidth;

        public float ActualWidth => Width.currentValue.Round(2);

        public float ActualHeight => Height.currentValue.Round(2);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public double NativeHeight
        {
            get
            {
                if (Native is null) return 0;

                return Thread.UI.Run(() =>
                {
#if IOS
                    return ((Native as UIKit.UIView)?.Frame.Height) ?? 0;
#else
                    var value = new[] { "Actual", "" }.Select(p => Native.GetType().GetProperty(p + "Height")?
                     .GetValue(Native)).ExceptNull().First();

                    return ((double)value).Round(1);
#endif
                });
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public double NativeWidth
        {
            get
            {
                if (Native is null) return 0;

                return Thread.UI.Run(() =>
               {
#if IOS
                   return ((Native as UIKit.UIView)?.Frame.Width) ?? 0;
#else
                   var value = new[] { "Actual", "" }.Select(p => Native.GetType().GetProperty(p + "Width")?.GetValue(Native)).ExceptNull().First();
                   return value.ToString().To<double>().Round(1);
#endif
               });
            }
        }

        public virtual void BindAutoWidthFromContent()
        {
            Log.For(this).Error("Auto Width from content is not supported in " + GetType().GetProgrammingName());
        }
    }
}
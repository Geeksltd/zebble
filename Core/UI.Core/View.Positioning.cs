namespace Zebble
{
    using System;
    using System.Linq;
    using Olive;

    [EscapeGCop("X and Y are acceptable names here.")]
    partial class View
    {
        void OnHorizontalMarginChanged()
        {
            if (!(parent is Stack) && parent?.Width.AutoOption == Length.AutoStrategy.Content)
                parent.Width.Update();

            if (X.IsUnknown && IsRendered() && IsAddedToNativeParentOnce) RaiseBoundsChanged();
        }

        void OnVerticalMarginChanged()
        {
            if (!(parent is Stack) && parent?.Height.AutoOption == Length.AutoStrategy.Content)
                parent.Height.Update();

            if (X.IsUnknown && IsRendered() && IsAddedToNativeParentOnce) RaiseBoundsChanged();
        }

        public float ActualX
        {
            get
            {
                if (!X.IsUnknown) return X.currentValue;
                return Margin.Left() + parent?.Effective.BorderAndPaddingLeft() ?? 0;
            }
        }

        public float ActualY
        {
            get
            {
                if (!Y.IsUnknown) return Y.currentValue;
                return Margin.Top() + parent?.Effective.BorderAndPaddingTop() ?? 0;
            }
        }

        /// <summary>
        /// Gets the X of this view from the top left corner of the device screen. 
        /// </summary>
        public float CalculateAbsoluteX()
        {
            return WithAllParents().Sum(b => b.ActualX) - GetAllParents().OfType<ScrollView>().Sum(x => x.ScrollX);
        }

        /// <summary>
        /// Gets the Y of this view from the top left corner of the device screen. 
        /// </summary>
        public float CalculateAbsoluteY()
        {
            return WithAllParents().Sum(b => b.ActualY) - GetAllParents().OfType<ScrollView>().Sum(x => x.ScrollY);
        }

        /// <summary>
        ///  Gets the current X of the native Object. Useful to check the progress of animations.
        /// </summary>
        public float NativeX
        {
            get
            {
                var native = Native; if (native is null) return -1;

                return Thread.UI.Run(() =>
                {
#if UWP
                    return (float)Windows.UI.Xaml.Controls.Canvas.GetLeft((Windows.UI.Xaml.UIElement)native);
#elif IOS

                    var nativeLayer = (native as UIKit.UIView).Layer.PresentationLayer;
                    if (nativeLayer is null) return -1;
                    return (float)nativeLayer.Frame.X;
#elif ANDROID
                    return Device.Scale.ToZebble((native as Android.Views.View).TranslationX);
#else
                    throw new NotSupportedException();
#endif
                });
            }
        }

        /// <summary>
        ///  Gets the current Y of the native Object. Useful to check the progress of animations.
        /// </summary>
        public float NativeY
        {
            get
            {
                var native = Native; if (native is null) return -1;
                return Thread.UI.Run(() =>
                {
#if UWP
                    return (float)Windows.UI.Xaml.Controls.Canvas.GetTop((Windows.UI.Xaml.UIElement)native);
#elif IOS
                    var nativeLayer = (native as UIKit.UIView).Layer.PresentationLayer;
                    if (nativeLayer is null) return -1f;
                    return (float)nativeLayer.Frame.Y;
#elif ANDROID
                    return Device.Scale.ToZebble((native as Android.Views.View).TranslationY);
#else
                    throw new NotSupportedException();
#endif
                });
            }
        }
    }
}
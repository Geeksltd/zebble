using System;
using Olive;

namespace Zebble
{
    partial class ScrollView
    {
        bool enableZooming;
        float minZoomScale = 1, maxZoomScale = 10;

        internal readonly AsyncEvent ZoomSettingsChanged = new();

        public bool EnableZooming
        {
            get => enableZooming;
            set
            {
                if (enableZooming != value)
                {
                    if (IsRendered())
                        throw new RenderException("ScrollView's EnableZooming cannot be changed after it's rendered.");
                    enableZooming = value;
                    ZoomSettingsChanged.Raise();
                }
            }
        }

        public float MinZoomScale
        {
            get => minZoomScale;
            set
            {
                if (!minZoomScale.AlmostEquals(value))
                {
                    minZoomScale = value;
                    ZoomSettingsChanged.Raise();
                }
            }
        }

        public float MaxZoomScale
        {
            get => maxZoomScale;
            set
            {
                if (!maxZoomScale.AlmostEquals(value))
                {
                    maxZoomScale = value;
                    ZoomSettingsChanged.Raise();
                }
            }
        }
    }
}
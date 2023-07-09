namespace Zebble
{
    using CoreAnimation;
    using CoreGraphics;
    using Foundation;
    using IOS;
    using System;
    using System.Linq;
    using Olive;

    partial class Renderer
    {
        IosImageView BackgroundImage;

        CAGradientLayer BackgroundColourLayer;
        CALayer BackgroundImageLayer;

        void SetBackgroundImage()
        {
            if (IsDead(out View view)) return;

            if (view is ImageView) return;
            if (!view.Effective.HasBackgroundImage()) return;

            if (BackgroundImage is null)
            {
                BackgroundImage = new IosImageView(view)
                {
                    Mirror = BackgroundImageLayer = new CALayer()
                };

                Layer?.InsertSublayer(BackgroundImageLayer, BackgroundColourLayer is null ? 0 : 1);
                Cling(BackgroundImageLayer);
            }

            BackgroundImage.LoadImage();
        }

        void Cling(CALayer layer)
        {
            if (IsDead(out View view)) return;
            void chase() => layer.Frame = new CGRect(0, 0, view.ActualWidth, view.ActualHeight);
            view.Width.Changed.HandleOnUI(chase);
            view.Height.Changed.HandleOnUI(chase);
            chase();
        }

        void SetBackgroundColor(UIChangedEventArgs<Color> args)
        {
            if (IsDead(out var view)) return;
            if (args.Value is not GradientColor color)
                args.Value = color = new GradientColor(args.Value, args.Value); // Otherwise gesture causes a bug.

            if (BackgroundColourLayer is null)
            {
                Layer?.InsertSublayer(BackgroundColourLayer = new CAGradientLayer(), 0);
                Cling(BackgroundColourLayer);
            }

            BackgroundColourLayer.StartPoint = color.StartPoint.Render();
            BackgroundColourLayer.EndPoint = color.EndPoint.Render();
            BackgroundColourLayer.Locations = color.Items.Select(c => c.StopAtPercentage.RenderPercentage()).ToArray();

            var colors = color.Items.Select(c => c.Color.ToCG()).ToArray();

            if (args.Animated())
                args.Animation.AddNative(BackgroundColourLayer, "colors", NSArray.FromObjects(colors));
            BackgroundColourLayer.Colors = colors;
        }
    }
}
namespace Zebble
{
    using System;
    using Android.Graphics.Drawables;
    using Android.Widget;
    using AndroidOS;
    using layout = Android.Views.ViewGroup.LayoutParams;
    using Olive;

    partial class Renderer
    {
        Android.Views.View BackgroundImage;

        void SetBackgroundAndBorder()
        {
            if (IsDead(out var view)) return;

            try { Result.OutlineProvider = null; } catch { /* Strange! */ }

            var hasGradient = view.BackgroundColor is GradientColor;
            var hasBorder = view.Effective.HasBorder();
            var hasComplexBorders = View.Effective.ShouldRenderBorderLines() && !View.Border.IsUniform();

            Drawable colorLayer;

            if (hasGradient || hasBorder)
                colorLayer = new BorderGradientDrawable(view);
            else colorLayer = new ColorDrawable(view.BackgroundColor.Render());

            if (hasComplexBorders)
                Result.Background = ZebbleBorderLayersDrawable.Create(view, colorLayer);
            else
                Result.Background = colorLayer;
        }

        void SetBackgroundImage()
        {
            if (IsDead(out var view)) return;
            if (view is ImageView) return;

            if (!view.Effective.HasBackgroundImage())
            {
                if (BackgroundImage != null)
                    BackgroundImage.Visibility = Android.Views.ViewStates.Gone;
            }
            else if (view.BackgroundImagePath.OrEmpty().EndsWith(".gif"))
            {
                if (Result is FrameLayout frame)
                    frame.AddView(new AndroidGifImageView(view as ImageView));
                else
                    throw new Exception("Gif images can only be used as Canvas Background.");
            }
            else
            {
                var container = Result as FrameLayout;
                if (container == null) return;

                if (BackgroundImage == null)
                {
                    var image = new AndroidImageView(view, backgroundImageOnly: true);
                    
                    image.Render().ContinueWith(x =>
                    {
                        Thread.UI.Post(() =>
                        {
                            if (x.IsFaulted || IsDead(out view) || !container.IsAlive()) return;
                            BackgroundImage = x.GetAlreadyCompletedResult();
                            BackgroundImage.LayoutParameters = new layout(layout.MatchParent, layout.MatchParent);
                            BackgroundImage.Visibility = Android.Views.ViewStates.Visible;
                            container.AddView(BackgroundImage, 0);
                        });
                    });
                }
                else BackgroundImage.Visibility = Android.Views.ViewStates.Visible;
            }
        }
    }
}
namespace Zebble.IOS
{
    using CoreGraphics;
    using Services;
    using System;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;
    using UIKit;
    using Olive;
    using System.Reflection.Emit;

    public class IosImageWrapper : UIView, UIChangeCommand.IHandler
    {
        ImageView View;
        public IosImageView NestedImage;

        public IosImageWrapper(ImageView view) : base(view.GetFrame()) { View = view; }

        public Task<IosImageWrapper> Render()
        {
            NestedImage = new IosImageView(View);
            Add(NestedImage);
            SyncInnerView();

            return Task.FromResult(this);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();
            SyncInnerView();
        }

        void SyncInnerView()
        {
            if (View?.Effective is null) return;
            if (NestedImage is null) return;
            if (Frame == null) return;

            NestedImage.Frame = View.GetEffectiveFrame();

            if (View.IsRendered()) NestedImage.LoadImage();
        }

        public void Apply(string property, UIChangedEventArgs change)
        {
            if (property == "Bounds") SyncInnerView();
        }

        protected override void Dispose(bool disposing)
        {
            View = null;
            base.Dispose(disposing);
        }
    }

    public partial class IosImageView : UIImageView
    {
        View View;
        ImageService.ImageSource Source;
        internal CoreAnimation.CALayer Mirror;

        public IosImageView(View view)
        {
            base.Frame = view.GetFrame();
            View = view;
            View.BackgroundImageChanged.HandleOnUI(LoadImage);
            View.BackgroundImageParametersChanged.HandleOnUI(SetImagePosition);
            UserInteractionEnabled = true;
            View.WhenShownOrPageRevisited(() => { try { LoadImage(); } catch { /* No logging needed */ } });
        }

        internal void LoadImage()
        {
            if (View?.IsDisposing != false)
                return;

            if (View.BackgroundImagePath.OrEmpty().EndsWith(".gif") || View.HasAnimatedBackgroundImage)
                Thread.UI.Run(SetGifAnimationLayers);
            else Layer.RemoveAllAnimations();

            var oldSource = Source;
            var newSource = View.BackgroundImageData.None() && View.BackgroundImagePath.HasValue() ? ImageService.GetSource(View) : null;
            if (oldSource != null)
            {
                if (oldSource == newSource) return; // No change.
                else oldSource.UnregisterViewer();
            }

            Source = newSource;

            ImageService.Draw(View, DrawImage);
        }

        void DrawImage(object imageObj)
        {
            if (imageObj == null)
                return;
            var image = imageObj as UIImage;
            if (View?.IsDisposing == false)
            {
                if (View.BackgroundImageStretch == Stretch.Fit)
                    Image = ChangeImageToFit(image, View.BackgroundImageAlignment);
                else
                    Image = image;
                SetImagePosition();

                if (Mirror is not null)
                    Mirror.Contents = Image.CGImage;
            }
        }

        UIImage ChangeImageToFit(UIImage image, Alignment alignment)
        {
            var imageWidth = (float)image.Size.Width;
            var imageHeight = (float)image.Size.Height;

            var frameWidth = (float)Math.Floor(Frame.Size.Width * Device.Screen.HardwareDensity);
            var frameHeight = (float)Math.Floor(Frame.Size.Height * Device.Screen.HardwareDensity);

            if (frameWidth == 0 || frameHeight == 0)
                return image;

            (var newWidth, var newHeight) = FindTheSuitableSize(imageWidth, imageHeight, frameWidth, frameHeight);

            (var x, var y) = GetRelativePosition(alignment, newWidth, newHeight, frameWidth, frameHeight);

            var scaleFactor = 1;
            if (frameWidth < imageWidth || frameHeight < imageHeight)
            {
                scaleFactor = ((int)Math.Ceiling(Math.Max(imageWidth / frameWidth, imageHeight / frameHeight))).LimitMax(1);
            }

            // scaling everything
            frameWidth *= scaleFactor;
            frameHeight *= scaleFactor;
            newWidth *= scaleFactor;
            newHeight *= scaleFactor;
            x *= scaleFactor;
            y *= scaleFactor;

            // Rounding up the dimensions
            frameWidth = (float)Math.Ceiling(frameWidth);
            frameHeight = (float)Math.Ceiling(frameHeight);
            newWidth = (float)Math.Ceiling(newWidth);
            newHeight = (float)Math.Ceiling(newHeight);

            //creating the image
            var rawData = new byte[(int)(frameWidth * frameHeight * 4)];
            var handle = GCHandle.Alloc(rawData);
            try
            {
                using var colorSpace = CGColorSpace.CreateDeviceRGB();
                using var newImage = new CGBitmapContext(rawData, (int)frameWidth, (int)frameHeight, 8, (int)(frameWidth * 4), colorSpace, CGBitmapFlags.PremultipliedLast);
                newImage.DrawImage(new CGRect(x, y, newWidth, newHeight), image.CGImage);
                return UIImage.FromImage(newImage.ToImage());
            }
            finally
            {
                handle.Free();
            }
        }

        (float width, float height) FindTheSuitableSize(float imageWidth, float imageHeight, float maxWidth, float maxHeight)
        {
            if (imageWidth <= 0 || imageHeight <= 0) return (0, 0); // to avoid DevideByZero

            var ratioW = maxWidth / imageWidth;
            var ratioH = maxHeight / imageHeight;
            var ratio = Math.Min(ratioW, ratioH);
            var suitableWidth = imageWidth * ratio;
            var suitableHeight = imageHeight * ratio;

            return (suitableWidth, suitableHeight);
        }

        (int x, int y) GetRelativePosition(Alignment alignment, float newWidth, float newHeight, float frameWidth, float frameHeight)
        {
            var left = 0; var center = (int)((frameWidth - newWidth) / 2); var right = (int)(frameWidth - newWidth);
            var top = 0; var middle = (int)((frameHeight - newHeight) / 2); var bottom = (int)(frameHeight - newHeight);

            switch (alignment)
            {
                case Alignment.Bottom:
                case Alignment.BottomMiddle: return (center, bottom);
                case Alignment.BottomLeft: return (left, bottom);
                case Alignment.BottomRight: return (right, bottom);
                case Alignment.Left: return (left, middle);
                case Alignment.TopLeft: return (left, top);
                case Alignment.Right: return (right, middle);
                case Alignment.TopRight: return (right, top);
                case Alignment.Top: return (center, top);
                default: return (center, middle);
            }
        }

        void SetImagePosition()
        {
            if (Image is null) return;
            Layer.MasksToBounds = true;

            var stretch = View.BackgroundImageStretch;

            switch (stretch)
            {
                case Stretch.Fill:
                    ContentMode = UIViewContentMode.ScaleToFill;
                    break;
                case Stretch.AspectFill:
                    ContentMode = UIViewContentMode.ScaleAspectFill;
                    break;
                case Stretch.Fit:
                    ContentMode = UIViewContentMode.ScaleAspectFit;
                    break;
                default:
                    ContentMode = View.BackgroundImageAlignment switch
                    {
                        Alignment.Bottom => UIViewContentMode.Bottom,
                        Alignment.Top => UIViewContentMode.Top,
                        Alignment.TopLeft => UIViewContentMode.TopLeft,
                        Alignment.TopRight => UIViewContentMode.TopRight,
                        Alignment.TopMiddle => UIViewContentMode.Top,
                        Alignment.Left => UIViewContentMode.Left,
                        Alignment.Right => UIViewContentMode.Right,
                        Alignment.Middle => stretch == Stretch.Fit ? UIViewContentMode.ScaleAspectFit : UIViewContentMode.Center,
                        Alignment.BottomLeft => UIViewContentMode.BottomLeft,
                        Alignment.BottomRight => UIViewContentMode.BottomRight,
                        Alignment.BottomMiddle => UIViewContentMode.Bottom,
                        _ => UIViewContentMode.ScaleAspectFit
                    };
                    break;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (Handle != IntPtr.Zero) Image = null;
                    Source?.UnregisterViewer();
                }
                catch
                {
                    // No logging is needed
                }
            }

            View = null;
            base.Dispose(disposing);
        }
    }
}
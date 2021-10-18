//namespace Zebble.IOS
//{
//    using CoreGraphics;
//    using Services;
//    using System;
//    using System.Runtime.InteropServices;
//    using System.Threading.Tasks;
//    using UIKit;
//    using Olive;
//    using CoreAnimation;

//    public class ImageLayer : CALayer
//    {
//        View View;
//        ImageService.ImageSource Source;

//        public ImageLayer(View view)
//        {
//            base.Frame = view.GetFrame();
//            View = view;
//            View.BackgroundImageChanged.HandleOnUI(LoadImage);
//            View.BackgroundImageParametersChanged.HandleOnUI(SetImagePosition);            
//            View.WhenShownOrPageRevisited(() => { try { LoadImage(); } catch { /* No logging needed */ } });
//        }

//        internal void LoadImage()
//        {
//             RemoveAllAnimations();

//            var oldSource = Source;
//            var newSource = View.BackgroundImageData.None() && View.BackgroundImagePath.HasValue() ? ImageService.GetSource(View) : null;
//            if (oldSource != null)
//            {
//                if (oldSource == newSource) return; // No change.
//                else oldSource.UnregisterViewer();
//            }

//            Source = newSource;

//            ImageService.Draw(View, DrawImage);
//        }

//        UIImage Image;

//        void DrawImage(object imageObj)
//        {
//            if (imageObj == null)
//                return;
//            var image = imageObj as UIImage;
//            if (View?.IsDisposing == false)
//            {
//                if (View.BackgroundImageStretch == Stretch.Fit)
//                    Image = ChangeImageToFit(image, View.BackgroundImageAlignment);
//                else
//                    Image = image;
//                SetImagePosition();
//            }
//        }

//        UIImage ChangeImageToFit(UIImage image, Alignment alignment)
//        {
//            var imageWidth = (float)image.Size.Width;
//            var imageHeight = (float)image.Size.Height;

//            var frameWidth = (float)Math.Floor(Frame.Size.Width * Device.Screen.HardwareDensity);
//            var frameHeight = (float)Math.Floor(Frame.Size.Height * Device.Screen.HardwareDensity);

//            if (frameWidth == 0 || frameHeight == 0)
//                return image;

//            (var newWidth, var newHeight) = CalculateNewDimensions(imageWidth, imageHeight, frameWidth, frameHeight);

//            (var x, var y) = GetRelativePosition(alignment, newWidth, newHeight, frameWidth, frameHeight);

//            var scaleFactor = 1;
//            if (frameWidth < imageWidth || frameHeight < imageHeight)
//            {
//                scaleFactor = ((int)Math.Ceiling(Math.Max(imageWidth / frameWidth, imageHeight / frameHeight))).LimitMax(1);
//            }

//            // scaling everything
//            frameWidth *= scaleFactor;
//            frameHeight *= scaleFactor;
//            newWidth *= scaleFactor;
//            newHeight *= scaleFactor;
//            x *= scaleFactor;
//            y *= scaleFactor;

//            // Rounding up the dimensions
//            frameWidth = (float)Math.Ceiling(frameWidth);
//            frameHeight = (float)Math.Ceiling(frameHeight);
//            newWidth = (float)Math.Ceiling(newWidth);
//            newHeight = (float)Math.Ceiling(newHeight);

//            //creating the image
//            var rawData = new byte[(int)(frameWidth * frameHeight * 4)];
//            var handle = GCHandle.Alloc(rawData);
//            try
//            {
//                using var colorSpace = CGColorSpace.CreateDeviceRGB();
//                using var newImage = new CGBitmapContext(rawData, (int)frameWidth, (int)frameHeight, 8, (int)(frameWidth * 4), colorSpace, CGBitmapFlags.PremultipliedLast);
//                newImage.DrawImage(new CGRect(x, y, newWidth, newHeight), image.CGImage);
//                return UIImage.FromImage(newImage.ToImage());
//            }
//            finally
//            {
//                handle.Free();
//            }
//        }

//        /*
//         * Do not try to refactor or simplify this method
//         * this amount of conditions make it much easier
//         * for debugging
//         */
//        (float width, float height) CalculateNewDimensions(float imageWidth, float imageHeight, float frameWidth, float frameHeight)
//        {
//            //Zebble.Device.Log.Warning($"{View.Id}- imageHeight: {imageHeight} - frameHeight: {frameHeight} - imageWidth: {imageWidth} - frameWidth: {frameWidth}");
//            if (imageHeight <= 0 || frameHeight <= 0) return (0, 0); // to avoid DevideByZero

//            float width = 0.0f, height = 0.0f;

//            var imageAspectRatio = imageWidth / imageHeight;
//            var frameAspectRatio = frameWidth / frameHeight;

//            if (imageWidth < frameWidth && imageHeight < frameHeight)
//            {
//                if (imageAspectRatio < frameAspectRatio)
//                {
//                    if (frameHeight < frameWidth)
//                    {
//                        height = frameHeight;
//                        width = frameHeight * imageAspectRatio;
//                    }
//                    else
//                    {
//                        width = frameWidth;
//                        height = frameWidth * imageAspectRatio;
//                    }
//                }
//                else
//                {
//                    width = frameWidth;
//                    height = imageHeight * (width / imageWidth);
//                }
//            }

//            else if (imageWidth > frameWidth && imageHeight > frameHeight)
//            {
//                if (imageAspectRatio < frameAspectRatio)
//                {
//                    height = frameHeight;
//                    width = frameHeight * imageAspectRatio;
//                }
//                else
//                {
//                    width = frameWidth;
//                    height = frameWidth * imageAspectRatio;
//                }
//            }

//            else if (imageWidth < frameWidth && imageHeight > frameHeight)
//            {
//                if (imageAspectRatio <= frameAspectRatio)
//                {
//                    height = frameHeight;
//                    width = imageWidth * (frameHeight / imageHeight);
//                }
//                else
//                {
//                    // Should not happen
//                    Log.For(this).Error($"1 -> ({imageWidth},{imageHeight}),({frameWidth},{frameHeight}) has IAR {imageAspectRatio} and FAR {frameAspectRatio}");
//                }
//            }

//            else if (imageWidth > frameWidth && imageHeight < frameHeight)
//            {
//                if (imageAspectRatio >= frameAspectRatio)
//                {
//                    height = frameHeight;
//                    width = imageWidth * (height / imageHeight);
//                }
//                else
//                {
//                    // Should not happen
//                    Log.For(this).Error($"2({imageWidth},{imageHeight}),({frameWidth},{frameHeight}) has IAR {imageAspectRatio} and FAR {frameAspectRatio}");
//                }
//            }
//            else if (imageWidth == frameWidth)
//            {
//                if (imageHeight <= frameHeight)
//                {
//                    width = frameWidth;
//                    height = imageHeight;
//                }
//                else
//                {
//                    height = frameHeight;
//                    width = imageWidth * (height / imageHeight);
//                }
//            }
//            else if (imageHeight == frameHeight)
//            {
//                if (imageWidth <= frameWidth)
//                {
//                    if (frameHeight < frameWidth)
//                    {
//                        height = frameHeight;
//                        width = frameHeight * imageAspectRatio;
//                    }
//                    else
//                    {
//                        width = frameWidth;
//                        height = frameWidth * imageAspectRatio;
//                    }
//                }
//                else
//                {
//                    width = frameWidth;
//                    height = imageHeight * (width / imageWidth);
//                }
//            }
//            else
//            {
//                Log.For(this).Error("The conditions do not cover all possibilities? Should not happen");
//            }

//            return (width, height);
//        }

//        (int x, int y) GetRelativePosition(Alignment alignment, float newWidth, float newHeight, float frameWidth, float frameHeight)
//        {
//            var left = 0; var center = (int)((frameWidth - newWidth) / 2); var right = (int)(frameWidth - newWidth);
//            var top = 0; var middle = (int)((frameHeight - newHeight) / 2); var bottom = (int)(frameHeight - newHeight);

//            switch (alignment)
//            {
//                case Alignment.Bottom:
//                case Alignment.BottomMiddle: return (center, bottom);
//                case Alignment.BottomLeft: return (left, bottom);
//                case Alignment.BottomRight: return (right, bottom);
//                case Alignment.Left: return (left, middle);
//                case Alignment.TopLeft: return (left, top);
//                case Alignment.Right: return (right, middle);
//                case Alignment.TopRight: return (right, top);
//                case Alignment.Top: return (center, top);
//                default: return (center, middle);
//            }
//        }

//        void SetImagePosition()
//        {
//            if (Image is null) return;
//            MasksToBounds = true;

//            var stretch = View.BackgroundImageStretch;

//            Image.ResizingMode = UIImageResizingMode.

//            if (stretch == Stretch.Fill) ContentMode = UIViewContentMode.ScaleToFill;
//            else if (stretch == Stretch.AspectFill) ContentMode = UIViewContentMode.ScaleAspectFill;
//            else if (stretch == Stretch.Fit) ContentMode = UIViewContentMode.ScaleAspectFit;
//            else
//            {
//                ContentMode = View.BackgroundImageAlignment switch
//                {
//                    Alignment.Bottom => UIViewContentMode.Bottom,
//                    Alignment.Top => UIViewContentMode.Top,
//                    Alignment.TopLeft => UIViewContentMode.TopLeft,
//                    Alignment.TopRight => UIViewContentMode.TopRight,
//                    Alignment.TopMiddle => UIViewContentMode.Top,
//                    Alignment.Left => UIViewContentMode.Left,
//                    Alignment.Right => UIViewContentMode.Right,
//                    Alignment.Middle => stretch == Stretch.Fit ? UIViewContentMode.ScaleAspectFit : UIViewContentMode.Center,
//                    Alignment.BottomLeft => UIViewContentMode.BottomLeft,
//                    Alignment.BottomRight => UIViewContentMode.BottomRight,
//                    Alignment.BottomMiddle => UIViewContentMode.Bottom,
//                    _ => UIViewContentMode.ScaleAspectFit
//                };
//            }
//        }

//        protected override void Dispose(bool disposing)
//        {
//            if (disposing)
//            {
//                try
//                {
//                    if (Handle != IntPtr.Zero) Image = null;
//                    Source?.UnregisterViewer();
//                }
//                catch
//                {
//                    // No logging is needed
//                }
//            }

//            View = null;
//            base.Dispose(disposing);
//        }
//    }
//}
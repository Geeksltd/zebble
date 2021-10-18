namespace Zebble.Services
{
    using CoreImage;
    using Foundation;
    using ImageIO;
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using UIKit;
    using Zebble;
    using Olive;

    partial class ImageService
    {
        static object ResizeLock = new object();

        public static async Task<UIImage> DecodeImage(FileInfo file, Size? desiredSize = null, Stretch stretch = Stretch.Default)
        {
            var bytes = await file.ReadAllBytesAsync();
            if (desiredSize == null || desiredSize.Value.Width <= 0) return await DecodeImage(bytes);
            else return await DecodeImage(bytes, desiredSize, stretch);
        }

        // resize the image (without trying to maintain aspect ratio)
        public static async Task<UIImage> DecodeImage(byte[] source, Size? desiredSize, Stretch stretch = Stretch.Default)
        {
            if (source.None()) throw new Exception("The provided image byte[] is empty.");
            var originalImage = await DecodeImage(source);

            if (originalImage == null || originalImage.CGImage == null)
                return new UIImage();

            var originalSize = new Size(originalImage.CGImage.Width, originalImage.CGImage.Height);

            if (desiredSize == null || desiredSize.Value.Width == 0 || desiredSize.Value.Height == 0)
                desiredSize = originalSize.Scale(1f / Device.Screen.HardwareDensity).RoundUp();

            var newSize = GetPixelSize(originalSize, desiredSize.Value, stretch);

            if (newSize.Scale(ImageSource.RESIZE_IF_LARGER_THAN).IsLargerThan(originalSize))
                return originalImage; // Close enough

            UIImage resultImage;
            lock (ResizeLock)
            {
                UIGraphics.BeginImageContext(newSize.Render());
                originalImage.Draw(new CoreGraphics.CGRect(0, 0, newSize.Width, newSize.Height));
                resultImage = UIGraphics.GetImageFromCurrentImageContext();
                UIGraphics.EndImageContext();
            }

            return resultImage;
        }

        public static Task<UIImage> DecodeImage(byte[] data)
        {
            try
            {
                var imageData = NSData.FromArray(data);
                return Task.FromResult(UIImage.LoadFromData(imageData, Device.Screen.HardwareDensity));
            }
            catch (Exception ex) { throw new Exception("Image Data is not valid." + ex); }
        }

        public static async Task Resize(FileInfo source, FileInfo destination, Size pixelSize,
            int jpegQuality = DEFAULT_JPEG_QUALITY)
        {
            var sourceImage = await DecodeImage(await source.ReadAllBytesAsync());

            UIGraphics.BeginImageContext(new CoreGraphics.CGSize(pixelSize.Width, pixelSize.Height));
            sourceImage.Draw(new CoreGraphics.CGRect(0, 0, pixelSize.Width, pixelSize.Height));

            using (var resizedImage = UIGraphics.GetImageFromCurrentImageContext())
            {
                UIGraphics.EndImageContext();

                byte[] data;
                if (destination.Extension.OrEmpty().ToLower().TrimStart(".").IsAnyOf("jpg", "jpeg"))
                    data = resizedImage.AsJPEG(jpegQuality * 0.01f).ToArray();
                else data = resizedImage.AsPNG().ToArray();

                await destination.WriteAllBytesAsync(data);
            }
        }

        internal static Size FindImageSize(FileInfo file)
        {
            using (var src = CGImageSource.FromUrl(NSUrl.FromFilename(file.FullName)))
            {
                var prop = src.GetProperties(0, new CGImageOptions { ShouldCache = false });

                #region Fix the orientation

                if (prop.Orientation.HasValue)
                {
                    var orientations = new[]
                    {
                        CIImageOrientation.LeftTop, CIImageOrientation.RightTop,
                        CIImageOrientation.RightBottom, CIImageOrientation.LeftBottom
                    };

                    if (orientations.Contains(prop.Orientation.Value))
                        return new Size(prop.PixelHeight.Value, prop.PixelWidth.Value);
                }

                #endregion

                return new Size(prop.PixelWidth.Value, prop.PixelHeight.Value);
            }
        }

        partial class ImageSource
        {
            public void Dispose()
            {
                var bmp = Image as UIImage;

                Image = null;
                if (bmp is null) return;

                bmp.Dispose();
                bmp = null;
            }
        }
    }
}
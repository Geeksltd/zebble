namespace Zebble.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Android.Graphics;
    using Android.Media;
    using Olive;

    partial class ImageService
    {
        public static async Task<Bitmap> DecodeImage(byte[] data, Size? desiredSize = null, Stretch stretch = Stretch.Default)
        {
            if (data.None()) throw new Exception("Failed to decode an image from null or empty byte[].");

            Bitmap result;

            if (desiredSize is null)
            {
                result = await BitmapFactory.DecodeByteArrayAsync(data, 0, data.Length);
                if (result is null) throw new Exception($"Failed to decode the specified byte[{data.Length}] into an Image.");
                return result;
            }

            var originalSize = FindImageSize(data);
            var newSize = GetPixelSize(originalSize, desiredSize.Value, stretch);

            result = await BitmapFactory.DecodeByteArrayAsync(data, 0, data.Length, new BitmapFactory.Options
            {
                InSampleSize = FindInSampleSize(originalSize, newSize).LimitMin(1),
                InScreenDensity = 1,
                InTargetDensity = 1
            });

            if (result is null) throw new Exception($"Failed to decode the specified byte[{data.Length}] into an Image.");

            return result;
        }

        static async Task<Bitmap> DoDecodeImage(FileInfo file, Size? desiredSize, Stretch stretch)
        {
            Bitmap result;

            var originalSize = GetPixelSize(file);

            if (desiredSize == null || desiredSize.Value.Width == 0 || desiredSize.Value.Height == 0)
                desiredSize = originalSize.Scale(1f / Device.Screen.HardwareDensity).RoundUp();

            var newSize = GetPixelSize(originalSize, desiredSize.Value, stretch);

            var options = CreateOptions(file);
            options.InDensity = options.InScreenDensity = options.InTargetDensity = 1;
            options.InSampleSize = FindInSampleSize(originalSize, newSize).LimitMin(1);

            result = await BitmapFactory.DecodeFileAsync(file.FullName, options);

            if (result is null) throw new Exception($"Failed to decode the specified image file: " + file.FullName);

            return result;
        }

        public static async Task<Bitmap> DecodeImage(FileInfo file, Size? desiredSize = null, Stretch stretch = Stretch.Default)
        {
            if (file is null) throw new ArgumentNullException(nameof(file));
            if (!await file.ExistsAsync()) throw new Exception("File not found: " + file.FullName);

            for (var retry = 3; retry > 0; retry--)
            {
                try { return await DoDecodeImage(file, desiredSize, stretch); }
                catch (Exception ex)
                {
                    if (retry == 1)
                        throw new Exception($"Failed to decode the image: {file.FullName}\n{ex.Message}", ex);
                    else
                    {
                        Nav.DisposeCache();
                        DisposeCache();
                        GC.Collect();
                        await Task.Delay(150);
                    }
                }
            }

            throw new Exception("This line will never run.");
        }

        public static Size FindImageSize(FileInfo file)
        {
            using (var options = new BitmapFactory.Options { InJustDecodeBounds = true })
            using (var bitmap = BitmapFactory.DecodeFile(file.FullName, options))
                return new Size(options.OutWidth, options.OutHeight);
        }

        internal static Size FindImageSize(byte[] data)
        {
            using (var options = new BitmapFactory.Options { InJustDecodeBounds = true })
            {
                BitmapFactory.DecodeByteArray(data, 0, data.Length, options);
                return new Size(options.OutWidth, options.OutHeight);
            }
        }

        public static Task Resize(FileInfo source, FileInfo destination, Size pixelSize,
            int jpegQuality = DEFAULT_JPEG_QUALITY)
        {
            return Thread.Pool.Run(() => DoResize(source, destination, pixelSize, jpegQuality));
        }

        static async Task DoResize(FileInfo source, FileInfo destination, Size size, int jpegQuality)
        {
            var sourceSize = GetPixelSize(source);

            var sampleSize = FindInSampleSize(sourceSize, size).LimitMin(1);

            var options = CreateOptions(source);
            options.InTargetDensity = options.InScreenDensity = options.InDensity = 1;
            options.InSampleSize = sampleSize;

            using (var bitMap = BitmapFactory.DecodeFile(source.FullName, options))
            using (await destination.GetSyncLock().Lock())
            using (var stream = destination.OpenWrite())
            {
                if (!await bitMap.CompressAsync(GetFormat(destination.Extension), jpegQuality, stream))
                    throw new Exception("Failed to compress the image!!");
                await stream.FlushAsync();

                bitMap.Recycle();
            }
        }

        static int FindInSampleSize(Size sourceSize, Size size)
        {
            if (sourceSize.Height < size.Height || sourceSize.Width < size.Width) return 0;

            var widthSample = Math.Floor(sourceSize.Width / size.Width);
            var heightSample = Math.Floor(sourceSize.Height / size.Height);

            return (int)Math.Min(widthSample, heightSample);
        }

        public static Task Rotate(FileInfo source, FileInfo destination, int degrees)
        {
            if (degrees == 0) return Task.CompletedTask;
            return Thread.Pool.Run(() => DoRotate(source, destination, degrees));
        }

        [EscapeGCop("Hard coded degree numbers are ok here.")]
        public static int FindExifRotationDegrees(FileInfo file)
        {
            try
            {
                using (var ei = new ExifInterface(file.FullName))
                {
                    var orientation = (Orientation)ei.GetAttribute(ExifInterface.TagOrientation).To<int>();

                    switch (orientation)
                    {
                        case Orientation.Rotate90: return 90;
                        case Orientation.Rotate270: return 270;
                        case Orientation.Rotate180: return 180;
                        default: return 0;
                    }
                }
            }
            catch
            {
                // No logging is needed.
                return 0;
            }
        }

        static async Task DoRotate(FileInfo source, FileInfo destination, int degrees,
            int resultJpegQuality = DEFAULT_JPEG_QUALITY)
        {
            var originalSize = GetPixelSize(source);
            const int MAX_PIXELS_FOR_MEMORY = 3000; // To prevent a memory issue.

            originalSize = originalSize.Scale(1f / Device.Screen.HardwareDensity).RoundUp();

            using (var sourceImage = await DecodeImage(source, originalSize
                .LimitTo(Size.Square(MAX_PIXELS_FOR_MEMORY / Device.Screen.HardwareDensity))))
            {
                var matrix = new Matrix().Set(x => x.PreRotate(degrees));

                using (var rotated = Bitmap.CreateBitmap(sourceImage, 0, 0,
                   sourceImage.Width, sourceImage.Height, matrix, filter: true))
                {
                    using (var stream = File.Open(destination.FullName, FileMode.Create, FileAccess.ReadWrite))
                    {
                        rotated.Compress(Bitmap.CompressFormat.Jpeg, resultJpegQuality, stream);
                        await stream.FlushAsync();
                        stream.Close();
                    }

                    rotated.Recycle();
                }

                sourceImage.Recycle();
            }

            using (var ei = new ExifInterface(destination.FullName))
            {
                var orientation = (Orientation?)ei.GetAttribute(ExifInterface.TagOrientation).TryParseAs<int>();

                if (orientation.HasValue && orientation != Orientation.Normal)
                {
                    ei.SetAttribute(ExifInterface.TagOrientation, ((int)Orientation.Normal).ToString());
                    ei.SaveAttributes();
                }
            }
        }

        static Bitmap.CompressFormat GetFormat(string extension)
        {
            extension = extension.OrEmpty().TrimStart(".").ToLower();
            switch (extension)
            {
                case "jpg": case "jpeg": return Bitmap.CompressFormat.Jpeg;
                case "png": return Bitmap.CompressFormat.Png;
                default: return Bitmap.CompressFormat.Webp;
            }
        }

        static BitmapFactory.Options CreateOptions(FileInfo source)
        {
            var result = new BitmapFactory.Options();

            if (source.Extension.ToLowerOrEmpty().IsAnyOf(".jpg", ".jpeg"))
            {
                // Reduce memory allocation as each pixel will be 2 bytes rather than 4.
                result.InPreferQualityOverSpeed = false;
                result.InPreferredConfig = Bitmap.Config.Rgb565;
            }

            return result;
        }

        partial class ImageSource
        {
            public void Dispose()
            {
                var bmp = Image as Bitmap;
                Image = null;
                if (!bmp.IsAlive()) return;

                bmp.Recycle();
                bmp.Dispose();
                bmp = null;
            }
        }
    }
}
namespace Zebble.Services
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;
    using Windows.Graphics.Imaging;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml.Media.Imaging;
    using Olive;

    partial class ImageService
    {
        public static async Task<BitmapImage> DecodeImage(byte[] imageData, Size? desiredSize = null, Stretch stretch = Stretch.Default)
        {
            var sourceImage = default(BitmapImage);
            var originalSize = default(Size);

            await Thread.UI.Run(async () =>
            {
                sourceImage = await DecodeImage(imageData);
                originalSize = new Size(sourceImage.PixelWidth, sourceImage.PixelHeight);
                if (desiredSize is null) desiredSize = originalSize;
                else desiredSize = GetPixelSize(originalSize, desiredSize.Value, stretch);
            });

            if (!desiredSize.Value.IsSmallerThan(originalSize))
                return sourceImage;

            var memStream = new MemoryStream(imageData);

            using (var imageStream = memStream.AsRandomAccessStream())
            {
                var decoder = await BitmapDecoder.CreateAsync(imageStream);
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                using (var resizedStream = new InMemoryRandomAccessStream())
                {
                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, resizedStream);

                    encoder.SetSoftwareBitmap(softwareBitmap);
                    encoder.BitmapTransform.ScaledWidth = (uint)desiredSize.Value.Width;
                    encoder.BitmapTransform.ScaledHeight = (uint)desiredSize.Value.Height;
                    encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;

                    await encoder.FlushAsync();

                    resizedStream.Seek(0);
                    var outBuffer = new byte[resizedStream.Size];
                    await resizedStream.ReadAsync(outBuffer.AsBuffer(), (uint)resizedStream.Size, InputStreamOptions.None);

                    return await DecodeImage(outBuffer);
                }
            }
        }

        public static async Task<BitmapImage> DecodeImage(FileInfo file, Size? desiredSize, Stretch stretch = Stretch.Default)
        {
            if (file is null) throw new ArgumentNullException(nameof(file));
            if (!await file.ExistsAsync()) throw new IOException("File not found: " + file.FullName);

            var fileData = await file.ReadAllBytesAsync();
            if (fileData.Length == 0) throw new BadDataException("Image file has zero bytes: " + file.FullName);

            return await Thread.UI.Run(doDecode);

            async Task<BitmapImage> doDecode()
            {
                using (var asStream = fileData.ToRandomAccessStream())
                {
                    var result = new BitmapImage();

                    try
                    {
                        await result.SetSourceAsync(asStream).AsTask();

                        var originalSize = new Size(result.PixelWidth, result.PixelHeight);
                        if (desiredSize is null) desiredSize = originalSize;
                        else desiredSize = GetPixelSize(originalSize, desiredSize.Value, stretch);
                        if (desiredSize?.Height > 0) result.DecodePixelHeight = (int)desiredSize.Value.Height;
                        if (desiredSize?.Width > 0) result.DecodePixelWidth = (int)desiredSize.Value.Width;
                    }
                    catch (Exception ex)
                    {
                        Log.For(typeof(ImageService)).Error(ex, "Failed to decode image from: " + file.FullName);
                    }

                    return result;
                }
            }
        }

        public static Task<BitmapImage> DecodeImage(byte[] imageBytes)
        {
            var result = Thread.UI.Run(async () =>
         {
             var bmp = new BitmapImage { CreateOptions = BitmapCreateOptions.IgnoreImageCache };
             await bmp.SetSourceAsync(imageBytes.ToRandomAccessStream());
             return bmp;
         });

            return result;
        }

        internal static Size FindImageSize(FileInfo file)
        {
            return Task.Factory.RunSync(() => FetchImageSize(file));
        }

        static async Task<Size> FetchImageSize(FileInfo file)
        {
            var get = await file.ToStorageFile().ConfigureAwait(false);
            var properties = await get.Properties.GetImagePropertiesAsync().AsTask().ConfigureAwait(false);

            return new Size(properties.Width, properties.Height);
        }

        public static async Task Resize(FileInfo source, FileInfo destination, Size pixelSize,
            int jpegQuality = DEFAULT_JPEG_QUALITY)
        {
            try
            {
                using var memStream = new MemoryStream(await source.ReadAllBytesAsync());
                using var imageStream = memStream.AsRandomAccessStream();
                var decoder = await BitmapDecoder.CreateAsync(imageStream);

                var pixelData = await decoder.GetPixelDataAsync();
                var detachedPixelData = pixelData.DetachPixelData();
                pixelData = null;

                using var resizedStream = new InMemoryRandomAccessStream();
                BitmapEncoder encoder;

                if (destination.Extension.ToLowerOrEmpty().TrimStart(".").IsAnyOf("jpg", "jpeg"))
                {
                    var propertySet = new BitmapPropertySet();
                    var qualityValue = new BitmapTypedValue(jpegQuality * 0.01, Windows.Foundation.PropertyType.Single);
                    propertySet.Add("ImageQuality", qualityValue);
                    encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, resizedStream, propertySet);

                    encoder.SetPixelData(decoder.BitmapPixelFormat, decoder.BitmapAlphaMode, decoder.OrientedPixelWidth, decoder.OrientedPixelHeight, decoder.DpiX, decoder.DpiY, detachedPixelData);
                }
                else
                {
                    encoder = await BitmapEncoder.CreateForTranscodingAsync(resizedStream, decoder);
                }

                encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Linear;
                encoder.BitmapTransform.ScaledHeight = (uint)pixelSize.Height;
                encoder.BitmapTransform.ScaledWidth = (uint)pixelSize.Width;

                await encoder.FlushAsync();
                resizedStream.Seek(0);
                var outBuffer = new byte[resizedStream.Size];
                await resizedStream.ReadAsync(outBuffer.AsBuffer(),
                    (uint)resizedStream.Size, InputStreamOptions.None);

                await destination.WriteAllBytesAsync(outBuffer);
            }
            catch (Exception ex)
            {
                Log.For(typeof(ImageService)).Warning("Failed to resize an image: " + source.FullName + " to " +
                    destination.FullName + Environment.NewLine + ex.Message);
            }
            finally
            {
                try
                {
                    if (await destination.ExistsAsync() && destination.FullName.AsFile().Length == 0)
                        destination.Delete();
                }
                catch
                {
                    // No logging is needed.
                }
            }
        }

        partial class ImageSource
        {
            public void Dispose() => Image = null;
        }
    }
}
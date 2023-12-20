namespace Zebble.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    partial class ImageService
    {
        public static Task<object> DecodeImage(byte[] data) => throw new NotSupportedException();

        internal static Size FindImageSize(FileInfo file) => throw new NotSupportedException();

        static Task<Size> FetchImageSize(FileInfo file) => throw new NotSupportedException();
        public static async Task<object> DecodeImage(FileInfo file, Size? desiredSize = null, Stretch stretch = Stretch.Default) => throw new NotSupportedException();

        public static async Task Resize(FileInfo source, FileInfo destination, Size pixelSize, int jpegQuality = DEFAULT_JPEG_QUALITY) => throw new NotSupportedException();

        partial class ImageSource
        {
            public void Dispose() => Image = null;
        }
    }
}
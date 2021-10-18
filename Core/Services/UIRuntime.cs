namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading.Tasks;
    using Olive;

    partial class UIRuntime
    {
        static Canvas pageContainer;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Canvas RenderRoot;

        public static object NativeRootScreen { get; set; }

        public static Canvas PageContainer
        {
            get => pageContainer ?? RenderRoot;
            set => pageContainer = value;
        }

        internal static async Task<View> Render(View view)
        {
            Services.IdleUITasks.ReportAction();

            view.Renderer = new Renderer(view);

            view.Native = await Thread.UI.Run(() => view.Renderer?.Render());

            await view.OnRendered();

            return view;
        }

        public static IEnumerable<KeyValuePair<string, Func<byte[]>>> GetEmbeddedResources()
        {
            foreach (var resource in AppAssembly.GetManifestResourceNames())
            {
                if (resource.EndsWith(".resources")) continue; // WPF internal

                yield return Pair.Of<string, Func<byte[]>>(resource, () => AppAssembly.ReadEmbeddedResource(resource));
            }
        }

        internal static Size ScaleImageSizeToFit(Size originalImage, Size size, Stretch stretch)
        {
            size.Width *= Device.Screen.Density;
            size.Height *= Device.Screen.Density;

            var width = originalImage.Width;
            var height = originalImage.Height;

            if (width <= size.Width && height <= size.Height) return originalImage;

            var originalImageRatio = width / height;
            var newNeededRatio = size.Width / size.Height;

            var isFit = stretch == Stretch.Fit;

            if (originalImageRatio > newNeededRatio) // original is wider, so the Width should fit.
            {
                if (isFit)
                {
                    width = size.Width; height = width / originalImageRatio;
                }
                else
                {
                    height = size.Height; width = height * originalImageRatio;
                }
            }
            else if (isFit)
            {
                height = size.Height; width = height * originalImageRatio;
            }
            else
            {
                width = size.Width; height = width / originalImageRatio;
            }

            return new Size(width, height);
        }

        public static bool IsTestMode => StartUp.Current?.IsTestMode() == true;
    }
}
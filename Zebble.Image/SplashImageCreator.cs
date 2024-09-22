namespace Zebble.Image
{
    using SkiaSharp;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Zebble.Tooling;
    using Olive;

    class SplashImageCreator
    {
        public class ScaledVersion
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public string OutputPath { get; set; }
            public bool ScaleToExactSize { get; set; } = false;

            public ScaledVersion() { }

            public ScaledVersion(int width, int height, string output)
            {
                Width = width;
                Height = height;
                OutputPath = output;
            }

        }

        [EscapeGCop("Hardcoded numbers are ok here.")]
        IEnumerable<ScaledVersion> GetVersions()
        {
            // Splash - iPhone
            yield return new ScaledVersion(375, 667, "Run\\iOS\\Resources\\Launch-Bg.png");
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Default.png", Width = 320, Height = 480 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Default@2x.png", Width = 640, Height = 960 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Default-568h@2x.png", Width = 640, Height = 1136 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Default-667h@2x.png", Width = 750, Height = 1334 };

            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\LaunchLogo@2x~ipad.png", Width = 1024, Height = 1024 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\LaunchLogo@2x~iphone.png", Width = 374, Height = 374 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\LaunchLogo@3x~iphone.png", Width = 621, Height = 621 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\LaunchLogo~ipad.png", Width = 384, Height = 384 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\LaunchLogo~iphone.png", Width = 320, Height = 320 };

            // Splash - iPad
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Default~ipad.png", Width = 768, Height = 1004 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Default768x1024.png", Width = 768, Height = 1024 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Default-Portrait.png", Width = 768, Height = 1024 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Default-Landscape~ipad.png", Width = 1024, Height = 748 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Default1024x768.png", Width = 1024, Height = 768 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Default-Landscape.png", Width = 1024, Height = 748 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\1024x1024.png", Width = 1024, Height = 1024 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Default-Portrait-736h@3x.png", Width = 1242, Height = 2208 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Default-Portrait@2x~ipad.png", Width = 1536, Height = 2048 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Default-Portrait@3x~ipad.png", Width = 2048, Height = 1536 };
            // Legacy
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Legacy-Icon-1024-768.png", Width = 1024, Height = 768 };

            // Splash - WinUI
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Splash.scale-400.png", Width = 2480, Height = 1200 };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Splash.scale-200.png", Width = 1240, Height = 600 };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Splash.scale-150.png", Width = 930, Height = 450 };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Splash.scale-125.png", Width = 775, Height = 375 };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Splash.scale-100.png", Width = 620, Height = 300 };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Wide310x150Logo.scale-400.png", Width = 1240, Height = 600, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Wide310x150Logo.scale-200.png", Width = 620, Height = 300, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Wide310x150Logo.scale-150.png", Width = 465, Height = 225, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Wide310x150Logo.scale-125.png", Width = 388, Height = 188, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Wide310x150Logo.scale-100.png", Width = 310, Height = 150, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square150x150Logo.scale-200.png", Width = 300, Height = 300, ScaleToExactSize = true };

            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.targetsize-24_altform-unplated.png", Width = 24, Height = 24, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\StoreLogo.png", Width = 50, Height = 50, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.scale-200.png", Width = 88, Height = 88, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\LargeTile.scale-100.png", Width = 310, Height = 310, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\LargeTile.scale-125.png", Width = 388, Height = 388, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\LargeTile.scale-150.png", Width = 465, Height = 465, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\LargeTile.scale-200.png", Width = 620, Height = 620, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\LargeTile.scale-400.png", Width = 1240, Height = 1240, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\LockScreenLogo.scale-200.png", Width = 48, Height = 48, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\SmallTile.scale-100.png", Width = 71, Height = 71, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\SmallTile.scale-125.png", Width = 89, Height = 89, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\SmallTile.scale-150.png", Width = 107, Height = 107, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\SmallTile.scale-200.png", Width = 142, Height = 142, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\SmallTile.scale-400.png", Width = 284, Height = 284, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square150x150Logo.scale-100.png", Width = 150, Height = 150, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square150x150Logo.scale-125.png", Width = 188, Height = 188, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square150x150Logo.scale-150.png", Width = 225, Height = 225, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square150x150Logo.scale-200.png", Width = 300, Height = 300, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square150x150Logo.scale-400.png", Width = 600, Height = 600, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.altform-unplated_targetsize-16.png", Width = 16, Height = 16, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.altform-unplated_targetsize-24.png", Width = 24, Height = 24, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.altform-unplated_targetsize-256.png", Width = 256, Height = 256, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.altform-unplated_targetsize-32.png", Width = 32, Height = 32, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.altform-unplated_targetsize-48.png", Width = 48, Height = 48, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.scale-100.png", Width = 44, Height = 44, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.scale-125.png", Width = 55, Height = 55, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.scale-150.png", Width = 66, Height = 66, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.scale-200.png", Width = 88, Height = 88, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.scale-400.png", Width = 176, Height = 176, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.targetsize-16.png", Width = 16, Height = 16, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.targetsize-24.png", Width = 24, Height = 24, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.targetsize-24_altform-unplated.png", Width = 24, Height = 24, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.targetsize-256.png", Width = 256, Height = 256, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.targetsize-32.png", Width = 32, Height = 32, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\Square44x44Logo.targetsize-48.png", Width = 48, Height = 48, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\StoreLogo.png", Width = 1240, Height = 600, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\StoreLogo.scale-100.png", Width = 50, Height = 50, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\StoreLogo.scale-125.png", Width = 63, Height = 63, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\StoreLogo.scale-150.png", Width = 75, Height = 75, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\StoreLogo.scale-200.png", Width = 100, Height = 100, ScaleToExactSize = true };
            yield return new ScaledVersion { OutputPath = "Run\\WinUI\\Assets\\Tiles\\StoreLogo.scale-400.png", Width = 200, Height = 200, ScaleToExactSize = true };


            // Splash - Android
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable\\Splash.png", Width = 2000, Height = 2000 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable\\Splash_Lg.png", Width = 1000, Height = 1000 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable-ldpi\\Splash.png", Width = 200, Height = 320 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable-mdpi\\Splash.png", Width = 320, Height = 480 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable-hdpi\\Splash.png", Width = 480, Height = 800 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable-xhdpi\\Splash.png", Width = 720, Height = 1280 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable-xxhdpi\\Splash.png", Width = 960, Height = 1600 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable-xxxhdpi\\Splash.png", Width = 1280, Height = 1920 };

            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable\\Splash.webp", Width = 2000, Height = 2000 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable-ldpi\\Splash.webp", Width = 200, Height = 320 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable-mdpi\\Splash.webp", Width = 320, Height = 480 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable-hdpi\\Splash.webp", Width = 480, Height = 800 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable-xhdpi\\Splash.webp", Width = 720, Height = 1280 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable-xxhdpi\\Splash.webp", Width = 960, Height = 1600 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable-xxxhdpi\\Splash.webp", Width = 1280, Height = 1920 };

            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable\\Icon.png", Width = 57 * 3, Height = 57 * 3 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable\\Icon.webp", Width = 57 * 3, Height = 57 * 3 };
            yield return new ScaledVersion { OutputPath = "Run\\Android\\Resources\\drawable\\Featured_Graphic.png", Width = 1024, Height = 500 };

            // Icons for iOS:
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Icon-Small.png", Width = 29, Height = 29 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Icon-Small@2x.png", Width = 58, Height = 58 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Icon-Small@3x.png", Width = 87, Height = 87 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Icon-40.png", Width = 40, Height = 40 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Icon-40@2x.png", Width = 80, Height = 80 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Icon-40@3x.png", Width = 120, Height = 120 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Icon-60.png", Width = 60, Height = 60 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Icon-60@2x.png", Width = 120, Height = 120 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Icon-60@3x.png", Width = 180, Height = 180 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Icon-76.png", Width = 76, Height = 76 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Icon-76@2x.png", Width = 152, Height = 152 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Icon-83.5@2x.png", Width = 167, Height = 167 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Icon-120.png", Width = 120, Height = 120 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\iTunesArtwork.png", Width = 512, Height = 512 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\iTunesArtwork@2x.png", Width = 1024, Height = 1024 };

            // Legacy iOS:
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Legacy-Icon-20.png", Width = 20, Height = 20 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Legacy-Icon-50.png", Width = 50, Height = 50 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Legacy-Icon-100.png", Width = 100, Height = 100 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Legacy-Icon-57.png", Width = 57, Height = 57 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Legacy-Icon-114.png", Width = 114, Height = 114 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Legacy-Icon-72.png", Width = 72, Height = 72 };
            yield return new ScaledVersion { OutputPath = "Run\\iOS\\Resources\\Legacy-Icon-144.png", Width = 144, Height = 144 };
        }

        internal void Run()
        {
            var sourceSplashPath = DirectoryContext.AppUIFolder.GetFile("Splash.png");
            var sourceIconPath = DirectoryContext.AppUIFolder.GetFile("Icon.png");

            if (!sourceSplashPath.Exists())
            {
                ConsoleHelpers.Error("Splash source file not found: " + sourceSplashPath.FullName); return;
            }

            if (!sourceIconPath.Exists())
            {
                ConsoleHelpers.Error("Icon source file not found: " + sourceIconPath.FullName); return;
            }

            using var sourceSplashImage = SKBitmap.Decode(sourceSplashPath.FullName);
            using var sourceIconImage = SKBitmap.Decode(sourceIconPath.FullName);

            Parallel.ForEach(GetVersions(), item =>
            {
                var useIcon = item.OutputPath.ContainsAny(new[] { "Icon", "Logo", "iTunesArtwork" }) || item.ScaleToExactSize;

                var source = useIcon ? sourceIconImage : sourceSplashImage;
                var savePath = DirectoryContext.RootFolder.FullName + "\\" + item.OutputPath;
                var isWebp = item.OutputPath.Contains("Android") && item.OutputPath.Contains(".webp");

                var size = Math.Min(item.Width, item.Height);

                using var scaled = source.Resize(new SKImageInfo(size, size), SKFilterQuality.High);

                using var expanded = new SKBitmap(item.Width, item.Height);
                expanded.Erase(source.Pixels[0]); // Fill background color

                using (var canvas = new SKCanvas(expanded))
                {
                    canvas.DrawBitmap(scaled, (item.Width - size) / 2, (item.Height - size) / 2);
                    canvas.Flush();
                }

                Console.WriteLine("Saving image to " + savePath);
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));

                using var image = SKImage.FromBitmap(expanded);
                using var stream = File.OpenWrite(savePath);

                using var encoder = image.Encode(isWebp ? SKEncodedImageFormat.Webp : SKEncodedImageFormat.Png, 100);
                encoder.SaveTo(stream);
            });
        }
    }
}
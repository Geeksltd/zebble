namespace Zebble.IOS
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CoreAnimation;
    using CoreGraphics;
    using Foundation;
    using ImageIO;
    using Olive;

    partial class IosImageView
    {
        void SetGifAnimationLayers()
        {
            CGImageSource sourceRef;

            if (View.BackgroundImageData.None())
            {
                var file = Device.IO.File(View.BackgroundImagePath);
                sourceRef = CGImageSource.FromData(NSData.FromFile(file.FullName));
            }
            else if (View.BackgroundImagePath.None())
            {
                sourceRef = CGImageSource.FromData(NSData.FromArray(View.BackgroundImageData));
            }
            else return;

            try { Layer.AddAnimation(CreateGifAnimationLayers(sourceRef), "content"); }
            catch (Exception ex) { Log.For(this).Error(ex, "CreateAnimationLayers."); }
        }

        CAKeyFrameAnimation CreateGifAnimationLayers(CGImageSource imageSource)
        {
            var frameCount = imageSource.ImageCount;
            if (frameCount == 0) throw new Exception("Gif has no image frames!");

            var frames = new List<GifFrameInfo>();

            for (var i = 0; i < frameCount; i++)
            {
                var frameData = imageSource.GetProperties(i, null).Dictionary["{GIF}"];
                frames.Add(new GifFrameInfo
                {
                    Image = CorrectImageSize(imageSource.CreateImage(i, null)),
                    Duration = frameData.ValueForKey("DelayTime".ToNs()).ToString().To<float>(),
                    StartTime = frames.Sum(x => x.Duration)
                });
            }

            var totalDuration = frames.Sum(x => x.Duration);
            frames.Do(x => x.StartTimePercent = x.StartTime / totalDuration);

            using (var gifProps = imageSource.GetProperties(null).Dictionary["{GIF}"])
            {
                var loopCount = gifProps.ValueForKey("LoopCount".ToNs()).ToString().To<float>();

                return new CAKeyFrameAnimation
                {
                    KeyPath = "contents",
                    Duration = totalDuration,
                    RepeatCount = loopCount <= 0 ? float.MaxValue : loopCount,
                    CalculationMode = CAAnimation.AnimationDescrete,
                    Values = frames.Select(x => x.Image.ToNs()).ToArray(),
                    KeyTimes = frames.Select(x => x.StartTimePercent.ToNs()).ToArray(),
                    RemovedOnCompletion = false
                };
            }
        }

        CGImage CorrectImageSize(CGImage inputImage)
        {
            var img = UIKit.UIImage.FromImage(inputImage);
            var correctSize = Services.ImageService.GetPixelSize(img.Size.ToZebble(), Frame.Size.ToZebble(), View.BackgroundImageStretch);
            if (img.Size != correctSize.Render())
                img = img.Scale(correctSize.Render(), scaleFactor: 1);

            return img.CGImage;
        }

        class GifFrameInfo
        {
            public CGImage Image;
            public float Duration, StartTime, StartTimePercent;
        }
    }
}
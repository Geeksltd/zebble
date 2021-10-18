namespace Zebble.AndroidOS
{
    using System;
    using System.Threading.Tasks;
    using Android.Graphics;
    using Android.OS;
    using Android.Runtime;
    using Gif;
    using Java.Lang;
    using Services;
    using Olive;

    public class AndroidGifImageView : Android.Widget.ImageView
    {
        const int FRAME_DEVIDE_TIME = 1000000;

        FramesExtractor Extractor;
        Bitmap CurrentFrame;
        new Handler Handler = new Handler(Looper.MainLooper);
        bool Animating, RenderFrame, ShouldClear, IsDisposed, IsImageScaled;
        Thread AnimationThread;
        long FramesDisplayDuration = -1L;
        byte[] Image;
        ImageView View;
        string Path;

        public AndroidGifImageView(ImageView view) : base(Renderer.Context) => View = view;

        [Preserve]
        public AndroidGifImageView(IntPtr ptr, JniHandleOwnership handle) : base(ptr, handle) { }

        public Task<AndroidGifImageView> Render()
        {
            Path = View.BackgroundImagePath;
            LoadImage();
            View.BackgroundImageChanged.HandleOnUI(LoadImage);

            return Task.FromResult(this);
        }

        void KillOldImage()
        {
            if (IsDead(out var view)) return;

            StopAnimation();

            Drawable?.Dispose();
            SetImageBitmap(null);

            if (IsImageScaled) KillImage(Image, Path);

            Image = null;
            IsImageScaled = false;
        }

        internal static void KillImage(byte[] image, string path)
        {
            if (ImageService.ShouldMemoryCache(path)) return;
            image = null;
        }

        void LoadImage()
        {
            if (IsDead(out var view)) return;

            KillOldImage();

            if (view.BackgroundImagePath.HasValue())
            {
                var file = Device.IO.File(Path);

                if (!file.Exists())
                {
                    Log.For(this).Error("Failed to load the image from " + Path + ".\r\nFile exists: " +
                      Device.IO.File(Path).Exists());
                }
                else if (!view.IsDisposing) // High concurrency 
                {
                    SetBytes(Image = file.ReadAllBytes());
                }
            }
            else if (view.BackgroundImageData?.Length > 0) SetBytes(view.BackgroundImageData);

            StartAnimation();
            SetScaleType(view.RenderImageAlignment());
        }

        void UpdateResults()
        {
            if (IsDead(out var view)) return;

            if (CurrentFrame?.IsRecycled == false)
                SetImageBitmap(CurrentFrame);
        }

        void CleanUp()
        {
            CurrentFrame = null;
            Extractor = null;
            AnimationThread = null;
            ShouldClear = false;
        }

        public void SetBytes(byte[] bytes)
        {
            Extractor = new FramesExtractor();
            try
            {
                Extractor.Read(bytes);
            }
            catch (Java.Lang.Exception ex)
            {
                Extractor = null;
                Log.For(this).Error(ex, "BaseGifView");
                return;
            }

            if (Animating) StartAnimationThread();
            else GotoFirstFrame();
        }

        void StartAnimation()
        {
            Animating = true;
            StartAnimationThread();
        }

        void StopAnimation()
        {
            Animating = false;

            if (AnimationThread != null)
            {
                AnimationThread.Interrupt();
                AnimationThread = null;
            }
        }

        void GotoFirstFrame()
        {
            if (Extractor.FramePointer == 0) return;
            if (Extractor.SetFrameIndex(-1) && !Animating)
            {
                RenderFrame = true;
                StartAnimationThread();
            }
        }

        public void Clear()
        {
            Animating = false;
            RenderFrame = false;
            ShouldClear = true;
            StopAnimation();
            Handler.Post(CleanUp);
        }

        bool CanStart() => (Animating || RenderFrame) && Extractor != null && AnimationThread == null;

        public void Run()
        {
            do
            {
                if (!Animating && !RenderFrame) break;

                var advance = Extractor.Advance();

                long frameDecodeTime = 0;
                try
                {
                    var before = DateTime.Now.Ticks;
                    CurrentFrame = Extractor.NextFrame();
                    frameDecodeTime = (DateTime.Now.Ticks - before) / FRAME_DEVIDE_TIME;
                    Handler.Post(UpdateResults);
                }
                catch (Java.Lang.Exception ex)
                {
                    Log.For(this).Error(ex, "BaseGifView");
                }

                RenderFrame = false;
                if (!Animating || !advance)
                {
                    Animating = false;
                    break;
                }

                try
                {
                    long delay = Extractor.GetNextDelay();
                    delay -= frameDecodeTime;
                    if (delay > 0) Thread.Sleep(FramesDisplayDuration > 0 ? FramesDisplayDuration : delay);
                }
                catch
                {
                    // No logging is needed
                }
            } while (Animating);

            if (ShouldClear) Handler.Post(CleanUp);

            AnimationThread = null;
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();
            Clear();
        }

        void StartAnimationThread()
        {
            if (CanStart())
            {
                AnimationThread = new Thread(Run);
                AnimationThread.Start();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                IsDisposed = true;
                KillOldImage();
                View = null;
            }

            base.Dispose(disposing);
        }

        bool IsDead(out ImageView view)
        {
            view = View;
            if (view?.IsDisposing == true || IsDisposed) view = null;
            return view is null;
        }
    }
}
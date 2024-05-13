using Android.Graphics;
using Android.Runtime;
using System;

using Android.Content;
using Android.Renderscripts;
using AndroidCanvas = Android.Graphics.Canvas;
using AndroidView = Android.Views.View;
using Zebble.Device;

namespace Zebble.AndroidOS
{
    public class AndroidBlurBox : AndroidBaseContainer<BlurBox>
    {
        RenderEffect BlurEffect;

        public AndroidBlurBox(BlurBox view) : base(view)
        {
            view.BlurredChanged.HandleOnUI(MaintainBlur);
            CreateBlur();
            MaintainBlur();
        }

        [Preserve]
        protected AndroidBlurBox(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

        void CreateBlur()
        {
            if (OS.IsAtLeast(Android.OS.BuildVersionCodes.S))
                BlurEffect = RenderEffect.CreateBlurEffect(15, 15, Shader.TileMode.Clamp);
        }

        void MaintainBlur()
        {
            if (IsDead(out var view)) return;
            if (OS.IsAtLeast(Android.OS.BuildVersionCodes.S))
                SetRenderEffect(view.Blurred ? BlurEffect : null);
        }

        bool IsDead(out BlurBox result)
        {
            result = View;
            if (result is null || result.IsDisposing) return true;
            return result.IsDisposing;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                View?.BlurredChanged.RemoveActionHandler(MaintainBlur);
                BlurEffect?.Dispose();
                BlurEffect = null;
            }

            base.Dispose(disposing);
        }
    }

    public class AndroidLegacyBlurBox : AndroidBaseContainer<BlurBox>
    {
        RenderScriptBlur Algorithm;
        PreDrawBlurController BlurController;

        public AndroidLegacyBlurBox(BlurBox view) : base(view)
        {
            view.BlurredChanged.HandleOnUI(MaintainBlur);
            CreateBlur();
            MaintainBlur();
        }

        [Preserve]
        protected AndroidLegacyBlurBox(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

        void CreateBlur()
        {
            Algorithm = new RenderScriptBlur(UIRuntime.AppContext);
            BlurController = new PreDrawBlurController(this, Algorithm);
        }

        void MaintainBlur()
        {
            if (IsDead(out var view)) return;
            BlurController?.SetBlurEnabled(view.Blurred);
        }

        protected override void DispatchDraw(AndroidCanvas canvas)
        {
            base.DispatchDraw(canvas);
            BlurController?.Draw(canvas);
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            BlurController?.UpdateBlurViewSize();
        }

        bool IsDead(out BlurBox result)
        {
            result = View;
            if (result is null || result.IsDisposing) return true;
            return result.IsDisposing;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !IsDisposed)
            {
                View?.BlurredChanged.RemoveActionHandler(MaintainBlur);
                BlurController?.Destroy();
                BlurController = null;
            }

            base.Dispose(disposing);
        }

        class RenderScriptBlur
        {
            readonly RenderScript RenderScript;
            readonly ScriptIntrinsicBlur BlurScript;

            Allocation OutAllocation;

            int LastWidth = -1;
            int LastHeight = -1;

            public RenderScriptBlur(Context context)
            {
                RenderScript = RenderScript.Create(context);
                BlurScript = ScriptIntrinsicBlur.Create(RenderScript, Element.U8_4(RenderScript));
            }

            bool CanReuseAllocation(Bitmap bitmap)
            {
                return bitmap.Height != LastHeight || bitmap.Width != LastWidth;
            }

            public Bitmap Blur(Bitmap bitmap)
            {
                using (var inAllocation = Allocation.CreateFromBitmap(RenderScript, bitmap))
                {
                    if (CanReuseAllocation(bitmap))
                    {
                        OutAllocation?.Destroy();
                        OutAllocation = Allocation.CreateTyped(RenderScript, inAllocation.Type);

                        LastWidth = bitmap.Width;
                        LastHeight = bitmap.Height;
                    }

                    BlurScript.SetRadius(15f);
                    BlurScript.SetInput(inAllocation);
                    BlurScript.ForEach(OutAllocation);
                    OutAllocation.CopyTo(bitmap);

                    return bitmap;
                }
            }

            public void Destroy()
            {
                BlurScript.Destroy();
                RenderScript.Destroy();
                OutAllocation?.Destroy();
            }
        }

        class PreDrawBlurController : Java.Lang.Object
        {
            readonly Paint Paint = new(PaintFlags.FilterBitmap | PaintFlags.AntiAlias);
            readonly RenderScriptBlur Algorithm;
            BlurViewCanvas InternalCanvas;
            Bitmap InternalBitmap;

            readonly AndroidView BlurView;

            bool BlurEnabled = true;
            bool Initialized;

            public PreDrawBlurController(AndroidView blurView, RenderScriptBlur blurAlgorithm)
            {
                BlurView = blurView;
                Algorithm = blurAlgorithm;

                int measuredWidth = blurView.MeasuredWidth;
                int measuredHeight = blurView.MeasuredHeight;

                Init(measuredWidth, measuredHeight);
            }

            void Init(int measuredWidth, int measuredHeight)
            {
                if (measuredWidth == 0 || measuredHeight == 0)
                {
                    BlurView.SetWillNotDraw(true);
                    return;
                }

                BlurView.SetWillNotDraw(false);

                InternalBitmap?.Dispose();
                InternalCanvas?.Dispose();

                InternalBitmap = Bitmap.CreateBitmap(measuredWidth, measuredHeight, Bitmap.Config.Argb8888);
                InternalCanvas = new BlurViewCanvas(InternalBitmap);
                Initialized = true;

                UpdateBlur();
            }

            void UpdateBlur()
            {
                if (BlurEnabled == false) return;
                if (Initialized == false) return;

                InternalBitmap.EraseColor(Colors.Transparent.Render());

                InternalCanvas.Save();
                BlurView.Draw(InternalCanvas);
                InternalCanvas.Restore();

                InternalBitmap = Algorithm.Blur(InternalBitmap);
            }

            public void Draw(AndroidCanvas canvas)
            {
                if (BlurEnabled == false) return;
                if (Initialized == false) return;
                if (canvas is BlurViewCanvas) return;

                canvas.DrawColor(Colors.Transparent.Render(), PorterDuff.Mode.Screen);
                canvas.DrawBitmap(InternalBitmap, 0f, 0f, Paint);
                canvas.Save();
            }

            public void UpdateBlurViewSize()
            {
                int measuredWidth = BlurView.MeasuredWidth;
                int measuredHeight = BlurView.MeasuredHeight;

                Init(measuredWidth, measuredHeight);
            }

            public void Destroy()
            {
                Algorithm.Destroy();
                Initialized = false;
            }

            public void SetBlurEnabled(bool enabled)
            {
                BlurEnabled = enabled;
                UpdateBlur();
                BlurView.Invalidate();
            }

            class BlurViewCanvas : AndroidCanvas
            {
                public BlurViewCanvas(Bitmap bitmap) : base(bitmap) { }
            }
        }
    }
}

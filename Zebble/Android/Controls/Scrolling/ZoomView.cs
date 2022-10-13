namespace Zebble.AndroidOS
{
    using System;
    using Android.Graphics;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using Olive;

    public class ZoomView : FrameLayout
    {
        readonly Zebble.ScrollView View;
        float Zoom = 1.0f, MaxZoom = 10.0f, SmoothZoom = 1.0f, ZoomX, ZoomY, MiniMapCaptionSize = 10.0f,
        SmoothZoomX, SmoothZoomY, TouchStartX, TouchStartY, TouchLastX, TouchLastY, Startd, Lastd, Lastdx1, Lastdy1, Lastdx2, Lastdy2;
        bool Scrolling, ShowMinimap, Pinching;
        int MiniMapColor = Color.Black, MiniMapHeight = -1, MiniMapCaptionColor = Color.White;
        string MiniMapCaption;
        double LastTapTime;
        readonly Matrix ZoomMatrix = new();
        readonly Paint Paint = new();
        Bitmap Snapshot;
        IZoomViewListener Listener;

        public ZoomView(Zebble.ScrollView view) : base(Renderer.Context)
        {
            View = view;
            LayoutDirection = LayoutDirection.Ltr;
        }

        [Preserve]
        public ZoomView(IntPtr ptr, JniHandleOwnership handle) : base(ptr, handle) { }

        public float GetZoom() => Zoom;

        public float GetMaxZoom() => MaxZoom;

        public void SetMaxZoom(float maxZoom)
        {
            if (maxZoom < 1.0f) return;
            MaxZoom = maxZoom;
        }

        public void SetMiniMapEnabled(bool showMiniMap) => ShowMinimap = showMiniMap;

        public bool IsMiniMapEnabled() => ShowMinimap;

        public void SetMiniMapHeight(int miniMapHeight)
        {
            if (miniMapHeight < 0) return;
            MiniMapHeight = miniMapHeight;
        }

        public int GetMiniMapHeight() => MiniMapHeight;

        public void SetMiniMapColor(int color) => MiniMapColor = color;

        public int GetMiniMapColor() => MiniMapColor;

        public string GetMiniMapCaption() => MiniMapCaption;

        public void SetMiniMapCaption(string miniMapCaption) => MiniMapCaption = miniMapCaption;

        public float GetMiniMapCaptionSize() => MiniMapCaptionSize;

        public void SetMiniMapCaptionSize(float size) => MiniMapCaptionSize = size;

        public int GetMiniMapCaptionColor() => MiniMapCaptionColor;

        public void SetMiniMapCaptionColor(int color) => MiniMapCaptionColor = color;

        public void ZoomTo(float zoom, float xPosition, float yPosition)
        {
            View.Zoom = Zoom = Math.Min(zoom, MaxZoom);
            ZoomX = xPosition;
            ZoomY = yPosition;
            SmoothZoomTo(Zoom, xPosition, yPosition);
        }

        public void SmoothZoomTo(float zoom, float xPosition, float yPosition)
        {
            SmoothZoom = Clamp(1.0f, zoom, MaxZoom);
            SmoothZoomX = xPosition;
            SmoothZoomY = yPosition;
            Listener?.OnZoomStarted(SmoothZoom, xPosition, yPosition);
        }

        public IZoomViewListener GetListener() => Listener;

        public void SetListner(IZoomViewListener listener) => Listener = listener;

        public float GetZoomFocusX() => ZoomX * Zoom;

        public float GetZoomFocusY() => ZoomY * Zoom;

        public override bool DispatchTouchEvent(MotionEvent ev)
        {
            if (ev.PointerCount == 1) ProcessSingleTouchEvent(ev);

            if (ev.PointerCount == 2) ProcessDoubleTouchEvent(ev);

            RootView.Invalidate();
            Invalidate();

            return true;
        }

        void ProcessSingleTouchEvent(MotionEvent ev)
        {
            var xPosition = ev.GetX();
            var yPosition = ev.GetY();

            var width = MiniMapHeight * (float)Width / Height;
            var height = MiniMapHeight;
            var touchingMiniMap = xPosition >= 10.0f && xPosition <= 10.0f + width && yPosition >= 10.0f && yPosition <= 10.0f + height;

            if (ShowMinimap && SmoothZoom > 1.0f && touchingMiniMap) ProcessSingleTouchOnMinimap(ev);
            else ProcessSingleTouchOutsideMinimap(ev);
        }

        void ProcessSingleTouchOnMinimap(MotionEvent ev)
        {
            var xPosotion = ev.GetX();
            var yPosition = ev.GetY();
            var width = MiniMapHeight * (float)Width / Height;
            var height = MiniMapHeight;
            var zx = (xPosotion - 10.0f) / width * Width;
            var zy = (yPosition - 10.0f) / height * Height;
            SmoothZoomTo(SmoothZoom, zx, zy);
        }

        void ProcessSingleTouchOutsideMinimap(MotionEvent ev)
        {
            var xPosition = ev.GetX();
            var yPosition = ev.GetY();
            var lx = xPosition - TouchStartX;
            var ly = yPosition - TouchStartY;
            var last = (float)Hypotenuse(lx, ly);
            var dx = xPosition - TouchLastX;
            var dy = yPosition - TouchLastY;
            TouchLastX = xPosition;
            TouchLastY = yPosition;

            switch (ev.Action)
            {
                case MotionEventActions.Down:
                    TouchStartX = xPosition;
                    TouchStartY = yPosition;
                    TouchLastX = xPosition;
                    TouchLastY = yPosition;
                    dx = 0;
                    dy = 0;
                    lx = 0;
                    ly = 0;
                    Scrolling = false;
                    break;

                case MotionEventActions.Move:
                    if (Scrolling || (SmoothZoom > 1.0f && last > 30.0f))
                    {
                        if (!Scrolling)
                        {
                            Scrolling = true;
                            ev.Action = MotionEventActions.Cancel;
                            base.DispatchTouchEvent(ev);
                        }

                        SmoothZoomX -= dx / Zoom;
                        SmoothZoomY -= dy / Zoom;
                        return;
                    }

                    break;

                case MotionEventActions.Outside:
                case MotionEventActions.Up:

                    // tap
                    if (last < 30.0f)
                    {
                        // check double tap
                        if (LastTapTime > 0 && LocalTime.Now.TimeOfDay.TotalMilliseconds - LastTapTime < 500)
                        {
                            if (SmoothZoom == 1.0f) SmoothZoomTo(MaxZoom, xPosition, yPosition);
                            else SmoothZoomTo(1.0f, Width / 2.0f, Height / 2.0f);
                            LastTapTime = 0;
                            ev.Action = MotionEventActions.Cancel;
                            base.DispatchTouchEvent(ev);
                            return;
                        }
                        else LastTapTime = LocalTime.Now.TimeOfDay.TotalMilliseconds;

                        PerformClick();
                    }

                    break;

                default:
                    break;
            }

            ev.SetLocation(ZoomX + (xPosition - 0.5f * Width) / Zoom, ZoomY + (yPosition - 0.5f * Height) / Zoom);

            ev.GetX();
            ev.GetY();

            base.DispatchTouchEvent(ev);
        }

        void ProcessDoubleTouchEvent(MotionEvent ev)
        {
            var x1 = ev.GetX(0);
            var dx1 = x1 - Lastdx1;
            Lastdx1 = x1;
            var y1 = ev.GetY(0);
            var dy1 = y1 - Lastdy1;
            Lastdy1 = y1;
            var x2 = ev.GetX(1);
            var dx2 = x2 - Lastdx2;
            Lastdx2 = x2;
            var y2 = ev.GetY(1);
            var dy2 = y2 - Lastdy2;
            Lastdy2 = y2;

            var distance = (float)Hypotenuse(x2 - x1, y2 - y1);
            var dd = distance - Lastd;
            Lastd = distance;
            var ld = Math.Abs(distance - Startd);

#pragma warning disable GCop517 // '{0}()' returns a value but doesn't change the object. It's meaningless to call it without using the returned result.
            Math.Atan2(y2 - y1, x2 - x1);
#pragma warning restore GCop517 // '{0}()' returns a value but doesn't change the object. It's meaningless to call it without using the returned result.
            switch (ev.Action)
            {
                case MotionEventActions.Down:
                    Startd = distance;
                    Pinching = false;
                    break;

                case MotionEventActions.Move:
                    if (Pinching || ld > 30.0f)
                    {
                        Pinching = true;
                        var dxk = 0.5f * (dx1 + dx2);
                        var dyk = 0.5f * (dy1 + dy2);
                        SmoothZoomTo(Math.Max(1.0f, Zoom * distance / (distance - dd)), ZoomX - dxk / Zoom, ZoomY - dyk / Zoom);
                    }

                    break;

                case MotionEventActions.Up:
                default:
                    Pinching = false;
                    break;
            }

            ev.Action = MotionEventActions.Cancel;
            base.DispatchTouchEvent(ev);
        }

        float Clamp(float min, float value, float max) => Math.Max(min, Math.Min(value, max));

        float Lerp(float factorA, float factorB, float factorK) => factorA + (factorB - factorA) * factorK;

        float Bias(float factorA, float factorB, float factorK) => Math.Abs(factorB - factorA) >= factorK ? factorA + factorK * Math.Sign(factorB - factorA) : factorB;

        double Hypotenuse(float factorA, float factorB) => Math.Sqrt(Math.Pow(factorA, 2) + Math.Pow(factorB, 2));

        protected override void DispatchDraw(Canvas canvas)
        {
            View.Zoom = Zoom = Lerp(Bias(Zoom, SmoothZoom, 0.05f), SmoothZoom, 0.2f);
            SmoothZoomX = Clamp(0.5f * Width / SmoothZoom, SmoothZoomX, Width - 0.5f * Width / SmoothZoom);
            SmoothZoomY = Clamp(0.5f * Height / SmoothZoom, SmoothZoomY, Height - 0.5f * Height / SmoothZoom);

            ZoomX = Lerp(Bias(ZoomX, SmoothZoomX, 0.1f), SmoothZoomX, 0.35f);
            ZoomY = Lerp(Bias(ZoomY, SmoothZoomY, 0.1f), SmoothZoomY, 0.35f);
            if (!Zoom.AlmostEquals(SmoothZoom) && Listener != null) Listener.OnZooming(Zoom, ZoomX, ZoomY);

            var animating = Math.Abs(Zoom - SmoothZoom) > 0.0000001f
                   || Math.Abs(ZoomX - SmoothZoomX) > 0.0000001f || Math.Abs(ZoomY - SmoothZoomY) > 0.0000001f;

            if (ChildCount == 0) return;

            ZoomMatrix.SetTranslate(0.5f * Width, 0.5f * Height);
            ZoomMatrix.PreScale(Zoom, Zoom);
            ZoomMatrix.PreTranslate(-Clamp(0.5f * Width / Zoom, ZoomX, Width - 0.5f * Width / Zoom),
                    -Clamp(0.5f * Height / Zoom, ZoomY, Height - 0.5f * Height / Zoom));

            var view = GetChildAt(0);
            ZoomMatrix.PreTranslate(view.Left, view.Top);

            if (animating && Snapshot == null && AnimationCacheEnabled)
            {
                view.DrawingCacheEnabled = true;
                Snapshot = view.DrawingCache;
            }

            if (animating && AnimationCacheEnabled && Snapshot != null)
            {
                Paint.Color = Color.White;
                canvas.DrawBitmap(Snapshot, ZoomMatrix, Paint);
            }
            else
            {
                Snapshot = null;
                canvas.Save();
                canvas.Concat(ZoomMatrix);
                view.Draw(canvas);
                canvas.Restore();
            }

            if (ShowMinimap)
            {
                if (MiniMapHeight < 0) MiniMapHeight = Height / 4;

                canvas.Translate(10.0f, 10.0f);

                var width = MiniMapHeight * (float)Width / Height;
                var height = MiniMapHeight;
                canvas.DrawRect(0.0f, 0.0f, width, height, Paint);

                if (MiniMapCaption.HasValue())
                {
                    Paint.TextSize = MiniMapCaptionSize;
                    Paint.AntiAlias = true;
                    canvas.DrawText(MiniMapCaption, 10.0f, 10.0f + MiniMapCaptionSize, Paint);
                    Paint.AntiAlias = false;
                }

                var dx = width * ZoomX / Width;
                var dy = height * ZoomY / Height;
                canvas.DrawRect(dx - 0.5f * width / Zoom, dy - 0.5f * height / Zoom, dx + 0.5f * width / Zoom, dy + 0.5f * height / Zoom, Paint);

                canvas.Translate(-10.0f, -10.0f);
            }

            RootView.Invalidate();
            Invalidate();
        }

        public void OnApiZoomChanged()
        {
            Log.For(this).Error("ApiZoomChanged is not implemented for Android yet !");
        }
    }
}
namespace Zebble.AndroidOS
{
    using System;
    using System.Threading.Tasks;
    using Android.Graphics;
    using Android.Runtime;
    using Android.Views;
    using Android.Widget;
    using Services;
    using Zebble.Device;
    using Olive;

    public class AndroidImageFactory
    {
        internal static View Create(Zebble.ImageView view)
        {
            if (view.Path?.EndsWith(".gif") == true || view.HasAnimatedBackgroundImage)
                return new AndroidGifImageView(view).Render().GetAlreadyCompletedResult();
            else
                return new AndroidControlWrapper<AndroidImageView>(view, new AndroidImageView(view)).Render();
        }
    }

    public class AndroidImageView : ImageView, IZebbleAndroidControl, UIChangeCommand.IHandler
    {
        Zebble.View View;
        bool IsDisposed, BackgroundImageOnly;
        Bitmap RoundedBitmap;
        ImageService.ImageSource Source;
        readonly EventHandlerDisposer EventHandlerDisposer = new EventHandlerDisposer();
        string DrawnImageKey;

        public AndroidImageView(Zebble.View view, bool backgroundImageOnly = false) : base(UIRuntime.CurrentActivity)
        {
            View = view;
            BackgroundImageOnly = backgroundImageOnly;
            SetScaleType(View.RenderImageAlignment());
        }

        [Preserve]
        public AndroidImageView(IntPtr ptr, JniHandleOwnership handle) : base(ptr, handle) { }

        public async Task<View> Render()
        {
            View.BackgroundImageChanged.HandleOnUI(LoadImage);
            View.BackgroundImageParametersChanged.HandleOnUI(OnBackgroundImageParametersChanged);

            if (!BackgroundImageOnly)
            {
                BackgroundColorChanged(new UIChangedEventArgs<Zebble.Color>(View, View.BackgroundColor));
            }

            if (await ShouldEarlyLoad()) LoadImage();
            else
            {
                // Remote or large non-cached image
                Thread.UI.Post(() => LoadImage());
            }

            return this;
        }

        public void Apply(string property, UIChangedEventArgs change)
        {
            if (property == "BackgroundColor") BackgroundColorChanged((UIChangedEventArgs<Zebble.Color>)change);
        }

        async Task<bool> ShouldEarlyLoad()
        {
            if (ImageService.ShouldMemoryCache(View.BackgroundImagePath.OrEmpty())) return true;
            if (View.BackgroundImagePath.IsUrl()) return false;

            if (View.Width.CurrentValue <= 100 && View.Height.CurrentValue < 100)
                if (View.BackgroundImagePath.HasValue())
                    if (await ImageService.GetSource(View).File.ExistsAsync())
                        return true;

            return false;
        }

        string GetDrawingKey()
        {
            return new object[] {
                   View.BackgroundImagePath, View.BackgroundImageAlignment,
                   View.BackgroundImageStretch, View.ActualWidth, View.ActualHeight
            }.ToString("|");
        }

        void OnBackgroundImageParametersChanged()
        {
            if (View == null || View.IsDisposing || !View.IsRendered() || !View.IsShown) return;
            if (Source is null) return; // Not rendered yet.

            if (Source != ImageService.GetSource(View))
            {
                LoadImage();
            }
            else
            {
                if (DrawnImageKey.IsEmpty() || DrawnImageKey == GetDrawingKey()) return;

                // Just redraw the existing image
                var bitmap = (Drawable as Android.Graphics.Drawables.BitmapDrawable)?.Bitmap;
                if (bitmap is null) return;
                DrawImage(bitmap);
            }
        }

        void BackgroundColorChanged(UIChangedEventArgs<Zebble.Color> args)
        {
            (((View)Parent) ?? this).SetBackgroundColor(args.Value.Render());
        }

        void DrawImage(object imageObj)
        {
            if (!this.IsAlive()) return;
            DrawnImageKey = GetDrawingKey();

            var image = imageObj as Bitmap;

            if (View?.IsDisposing != false) return;
            if (!image.IsAlive()) return;

            try
            {
                SetScaleType(View.RenderImageAlignment(image));
                if (!BackgroundImageOnly && View.Effective.HasAnyBorderRadius()) image = RoundCorners(image);
                SetImageBitmap(image);
            }
            catch (Exception ex)
            {
                Log.For(this).Warning($"Failed to use the image for {View?.GetFullPath()}\n{ex.Message}");
            }
        }

        public override void SetImageBitmap(Bitmap bm)
        {
            if (bm != null) DisposeDrawable();
            base.SetImageBitmap(bm);
        }

        void LoadImage()
        {
            EventHandlerDisposer.DisposeAll();
            if (View == null || View.IsDisposing) return;

            var oldSource = Source;
            if (oldSource == null)
            {
                ImageService.Draw(View, DrawImage);
                return;
            }

            var newSource = View.BackgroundImageData.None() && View.BackgroundImagePath.HasValue() ? ImageService.GetSource(View) : null;
            if (oldSource == newSource) return; // No change.

            ImageService.Draw(View, DrawImage);
            oldSource.UnregisterViewer();
        }

        void DisposeDrawable()
        {
            try
            {
                var drawable = Drawable;
                SetImageBitmap(null);
                if (drawable.IsAlive()) drawable?.Dispose();
            }
            catch { /* No logging is needed */ }

            if (RoundedBitmap != null)
                try
                {
                    var link = RoundedBitmap;
                    RoundedBitmap = null;
                    if (link.IsAlive())
                    {
                        link?.Recycle();
                        link?.Dispose();
                    }
                }
                catch { /* No logging is needed */ }
        }

        protected override void Dispose(bool disposing)
        {
            EventHandlerDisposer.DisposeAll();

            if (disposing && !IsDisposed && this.IsAlive())
            {
                IsDisposed = true;
                DisposeDrawable();
                Source?.UnregisterViewer();
                Source = null;
                View = null;
            }

            base.Dispose(disposing);
        }

        Bitmap RoundCorners(Bitmap source)
        {
            source = ResizeImage(source);
            var newSource = DrawOnCenter(source);

            int radius;
            //It's circle
            if (View.Width.CurrentValue.AlmostEquals(View.Height.CurrentValue))
            {
                Bitmap squareBitmap;
                if (newSource.Width >= newSource.Height)
                    squareBitmap = Bitmap.CreateBitmap(newSource, newSource.Width / 2 - newSource.Height / 2, 0, newSource.Height, newSource.Height);
                else
                    squareBitmap = Bitmap.CreateBitmap(newSource, 0, newSource.Height / 2 - newSource.Width / 2, newSource.Width, newSource.Width);

                radius = squareBitmap.Width / 2;
                newSource = squareBitmap;
            }
            else
                radius = Scale.ToDevice(View.Effective.BorderRadiusBottomLeft());

            var result = Bitmap.CreateBitmap(newSource.Width, newSource.Height, Bitmap.Config.Argb8888);
            var rect = new Rect(0, 0, newSource.Width, newSource.Height);
            using (var shader = new BitmapShader(newSource, Shader.TileMode.Clamp, Shader.TileMode.Clamp))
            using (var paint = new Paint { AntiAlias = true })
            using (var canvas = new Canvas(result))
            {
                paint.SetShader(shader);
                canvas.DrawRoundRect(new RectF(rect), radius, radius, paint);
                paint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.SrcIn));
                canvas.DrawBitmap(result, rect, rect, paint);
            }

            result.PrepareToDraw();

            if (Source != null)
            {
                Source?.UnregisterViewer();
                Source = null;
            }

            RoundedBitmap = result.Copy(Bitmap.Config.Argb8888, isMutable: true);
            return result;
        }

        Bitmap ResizeImage(Bitmap image)
        {
            var maxWidth = Scale.ToDevice(View.Width.CurrentValue) * 2;
            var maxHeight = Scale.ToDevice(View.Height.CurrentValue) * 2;

            var width = image.Width;
            var height = image.Height;
            var ratioBitmap = width / (float)height;
            var ratioMax = maxWidth / (float)maxHeight;

            var finalWidth = maxWidth;
            var finalHeight = maxHeight;
            if (ratioMax > 1) finalWidth = (int)(maxHeight * ratioBitmap);
            else finalHeight = (int)(maxWidth / ratioBitmap);

            if (finalWidth <= 0 || finalHeight <= 0) return image;

            return Bitmap.CreateScaledBitmap(image, finalWidth, finalHeight, true);
        }

        Bitmap DrawOnCenter(Bitmap source)
        {
            var width = Scale.ToDevice(View.Width.CurrentValue);
            var height = Scale.ToDevice(View.Height.CurrentValue);

            if (source.Width < width || source.Height < height)
            {
                width = source.Width > width ? source.Width : width;
                height = source.Height > height ? source.Height : height;

                var transparentXThershould = Math.Abs(width - source.Width) / 2;
                var transparentYThershould = Math.Abs(height - source.Height) / 2;

                var result = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888);

                using (var paint = new Paint { AntiAlias = true })
                using (var canvas = new Canvas(result))
                    canvas.DrawBitmap(source, new Rect(0, 0, source.Width, source.Height),
                        new RectF(transparentXThershould, transparentYThershould, transparentXThershould + source.Width, transparentYThershould + source.Height), paint);

                result.PrepareToDraw();

                if (Source != null)
                {
                    Source?.UnregisterViewer();
                    Source = null;
                }

                return result;
            }

            return source;
        }

        [EscapeGCop("In this case an out parameter can improve the code.")]
        bool IsDead(out Zebble.View result)
        {
            result = View;
            if (result is null || IsDisposed) return true;
            return result.IsDisposing;
        }

        [EscapeGCop("The parameter names are good names.")]
        protected override bool SetFrame(int left, int top, int right, int bottom)
        {
            if (IsDead(out var view)) return false;

            if (GetScaleType() == ScaleType.Matrix && Drawable != null)
            {
                try
                {
                    var matrix = ImageMatrix;
                    var viewWidth = MeasuredWidth - (PaddingLeft + PaddingRight);
                    var viewHeight = MeasuredHeight - (PaddingTop + PaddingBottom);
                    var drawableWidth = Drawable.IntrinsicWidth;
                    var drawableHeight = Drawable.IntrinsicHeight;

                    RectF drawableRect = null;
                    RectF viewRect = null;

                    if (view.BackgroundImageStretch == Stretch.Default)
                    {
                        drawableRect = CalculateDefaultStretchOption(viewWidth, viewHeight, drawableWidth, drawableHeight);
                        viewRect = new RectF(0, 0, viewWidth, viewHeight);

                        if (view.BackgroundImageAlignment == Alignment.Left || view.BackgroundImageAlignment == Alignment.BottomLeft || View.BackgroundImageAlignment == Alignment.TopLeft)
                            matrix.SetRectToRect(drawableRect, viewRect, Matrix.ScaleToFit.Start);
                        else if (view.BackgroundImageAlignment == Alignment.Right || view.BackgroundImageAlignment == Alignment.BottomRight || View.BackgroundImageAlignment == Alignment.TopRight)
                            matrix.SetRectToRect(drawableRect, viewRect, Matrix.ScaleToFit.End);
                        else if (view.BackgroundImageAlignment == Alignment.Middle || view.BackgroundImageAlignment == Alignment.BottomMiddle || View.BackgroundImageAlignment == Alignment.TopMiddle)
                            matrix.SetRectToRect(drawableRect, viewRect, Matrix.ScaleToFit.Center);
                        else if (view.BackgroundImageAlignment == Alignment.Top || view.BackgroundImageAlignment == Alignment.Bottom)
                            matrix.SetRectToRect(drawableRect, viewRect, Matrix.ScaleToFit.Center);
                        else
                            matrix.SetRectToRect(drawableRect, viewRect, Matrix.ScaleToFit.Start);
                    }
                    else
                    {
                        viewRect = CalculateOtherStretchOptions(viewWidth, viewHeight, drawableWidth, drawableHeight);
                        drawableRect = new RectF(0, 0, drawableWidth, drawableHeight);
                        matrix.SetRectToRect(drawableRect, viewRect, Matrix.ScaleToFit.Fill);
                    }

                    ImageMatrix = matrix;
                }
                catch
                {
                    // It just runs for the first time when ImageView doesn't have an image yet.
                }
            }

            var effective = view.Effective;

            left = Scale.ToDevice(effective.BorderAndPaddingLeft());
            top = Scale.ToDevice(effective.BorderAndPaddingTop());
            var width = Scale.ToDevice(view.ActualWidth - view.HorizontalPaddingAndBorder());
            var height = Scale.ToDevice(view.ActualHeight - view.VerticalPaddingAndBorder());
            right = left + width;
            bottom = top + height;

            return base.SetFrame(left, top, right, bottom);
        }

        RectF CalculateOtherStretchOptions(int viewWidth, int viewHeight, int drawableWidth, int drawableHeight)
        {
            RectF result = null;
            var widthScaleFactor = (float)viewWidth / drawableWidth;
            var heightScaleFactor = (float)viewHeight / drawableHeight;
            var scaleFactor = Math.Max(widthScaleFactor, heightScaleFactor);

            var scaledWidth = drawableWidth * scaleFactor;
            var scaledHeight = drawableHeight * scaleFactor;
            var widthGap = viewWidth - scaledWidth;
            var heightGap = viewHeight - scaledHeight;

            switch (View.BackgroundImageAlignment)
            {
                case Alignment.Top:
                    result = new RectF(widthGap / 2, 0, scaledWidth, (viewHeight + scaledHeight) / 2);
                    break;
                case Alignment.TopLeft:
                    result = new RectF(0, 0, scaledWidth, scaledHeight);
                    break;
                case Alignment.TopRight:
                    result = new RectF(widthGap, 0, viewWidth, scaledHeight);
                    break;
                case Alignment.TopMiddle:
                    result = new RectF(widthGap / 2, 0, (viewWidth + scaledWidth) / 2, scaledHeight);
                    break;
                case Alignment.Bottom:
                    result = new RectF(widthGap / 2, heightGap, (viewWidth + scaledWidth) / 2, viewHeight);
                    break;
                case Alignment.BottomRight:
                    result = new RectF(widthGap, heightGap, viewWidth, viewHeight);
                    break;
                case Alignment.BottomMiddle:
                    result = new RectF(widthGap / 2, heightGap, (viewWidth + scaledWidth) / 2, viewHeight);
                    break;
                case Alignment.BottomLeft:
                    result = new RectF(0, heightGap, scaledWidth, viewHeight);
                    break;
                case Alignment.Left:
                    result = new RectF(0, heightGap / 2, scaledWidth, (viewHeight + scaledHeight) / 2);
                    break;
                case Alignment.Middle:
                    result = new RectF(widthGap / 2, heightGap / 2, (viewWidth + scaledWidth) / 2, (viewHeight + scaledHeight) / 2);
                    break;
                case Alignment.Right:
                    result = new RectF(widthGap, heightGap / 2, viewWidth, (viewHeight + scaledHeight) / 2);
                    break;
                default:
                    result = new RectF(0, 0, viewWidth, viewHeight);
                    break;
            }

            return result;
        }

        RectF CalculateDefaultStretchOption(int viewWidth, int viewHeight, int drawableWidth, int drawableHeight)
        {
            RectF drawableRect = null;
            float scale;

            // Get the scale
            if (drawableWidth * viewHeight > drawableHeight * viewWidth)
            {
                scale = (float)viewHeight / (float)drawableHeight;
            }
            else
            {
                scale = (float)viewWidth / (float)drawableWidth;
            }

            var widthScale = (viewWidth / scale);
            var heightScale = (viewHeight / scale);
            // Define the rect to take image portion from
            switch (View.BackgroundImageAlignment)
            {
                case Alignment.Top:
                    drawableRect = new RectF(drawableWidth / 3, 0, (drawableWidth / 3) * 2, drawableHeight / 3);
                    break;
                case Alignment.TopLeft:
                    drawableRect = new RectF(0, 0, drawableWidth / 3, drawableHeight / 3);
                    break;
                case Alignment.TopRight:
                    drawableRect = new RectF((drawableWidth / 3) * 2, 0, drawableWidth, drawableHeight / 3);
                    break;
                case Alignment.TopMiddle:
                    drawableRect = new RectF(drawableWidth / 3, 0, (drawableWidth / 3) * 2, drawableHeight / 3);
                    break;
                case Alignment.Bottom:
                    drawableRect = new RectF(drawableWidth / 3, (drawableHeight / 3) * 2, (drawableWidth / 3) * 2, drawableHeight);
                    break;
                case Alignment.BottomRight:
                    drawableRect = new RectF((drawableWidth / 3) * 2, (drawableHeight / 3) * 2, drawableWidth, drawableHeight);
                    break;
                case Alignment.BottomMiddle:
                    drawableRect = new RectF(drawableWidth / 3, (drawableHeight / 3) * 2, (drawableWidth / 3) * 2, drawableHeight);
                    break;
                case Alignment.BottomLeft:
                    drawableRect = new RectF(0, (drawableHeight / 3) * 2, drawableWidth / 3, drawableHeight);
                    break;
                case Alignment.Left:
                    drawableRect = new RectF(0, drawableHeight / 3, drawableWidth / 3, (drawableHeight / 3) * 2);
                    break;
                case Alignment.Middle:
                    drawableRect = new RectF(drawableWidth / 3, drawableHeight / 3, (drawableWidth / 3) * 2, (drawableHeight / 3) * 2);
                    break;
                case Alignment.Right:
                    drawableRect = new RectF((drawableWidth / 3) * 2, drawableHeight / 3, drawableWidth, (drawableHeight / 3) * 2);
                    break;
                default:
                    drawableRect = new RectF(0, 0, drawableWidth / 3, drawableHeight / 3);
                    break;
            }

            return drawableRect;
        }
    }
}
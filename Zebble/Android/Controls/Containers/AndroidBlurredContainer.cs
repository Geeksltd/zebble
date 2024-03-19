using Android.Graphics;
using Android.Runtime;
using System;

namespace Zebble.AndroidOS
{
    public class AndroidBlurredContainer : AndroidBaseContainer<Stack>
    {
        RenderEffect BlurEffect;

        public AndroidBlurredContainer(Stack view) : base(view)
        {
            view.BlurredChanged.HandleOnUI(MaintainBlur);
            CreateBlur();
            MaintainBlur();
        }

        [Preserve]
        protected AndroidBlurredContainer(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }

        void CreateBlur() => BlurEffect = RenderEffect.CreateBlurEffect(10, 10, Shader.TileMode.Clamp);

        void MaintainBlur()
        {
            if (IsDead(out var view)) return;
            SetRenderEffect(view.Blurred ? BlurEffect : null);
        }

        bool IsDead(out Stack result)
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
                BlurEffect.Dispose();
                BlurEffect = null;
            }

            base.Dispose(disposing);
        }
    }
}

using Android.Views;
using Zebble.Device;

namespace Zebble.AndroidOS
{
    class Gpu
    {
        const int SafetyMargin = 50;
        static int? maxWidth, maxHeight, maxAnimatableWidth, maxAnimatableHeight;

        public static int MaxWidth
        {
            get
            {
                if (maxWidth is null) Load();
                return maxWidth.Value;
            }
        }

        public static int MaxAnimatableWidth
        {
            get
            {
                if (maxAnimatableWidth is null) Load();
                return maxAnimatableWidth.Value;
            }
        }

        public static int MaxHeight
        {
            get
            {
                if (maxHeight is null) Load();
                return maxHeight.Value;
            }
        }

        public static int MaxAnimatableHeight
        {
            get
            {
                if (maxAnimatableHeight is null) Load();
                return maxAnimatableHeight.Value;
            }
        }

        static void Load()
        {
            using (var temp = new Android.Graphics.Canvas())
            {
                maxWidth = temp.MaximumBitmapWidth / 8;
                maxHeight = temp.MaximumBitmapHeight / 8;

                maxAnimatableWidth = maxWidth - Scale.ToDevice(View.Root.ActualWidth) - SafetyMargin;
                maxAnimatableHeight = maxHeight - Scale.ToDevice(View.Root.ActualHeight) - SafetyMargin;
            }
        }

        public static bool CanAnimate(Android.Views.View view)
        {
            if (view.Width + view.Left >= MaxAnimatableWidth) return false;
            if (view.Height + view.Top >= MaxAnimatableHeight) return false;

            if (view is ViewGroup container)
                for (var i = 0; i < container.ChildCount; i++)
                    if (!CanAnimate(container.GetChildAt(i))) return false;

            return true;
        }
    }
}
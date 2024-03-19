using Android.Runtime;
using System;

namespace Zebble.AndroidOS
{
    public class AndroidCustomContainer : AndroidBaseContainer<View>
    {
        public AndroidCustomContainer(View view) : base(view) { }

        [Preserve]
        protected AndroidCustomContainer(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) { }
    }
}

namespace Zebble.Device
{
    using System.Runtime.CompilerServices;

    public static class Scale
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToDevice(float originalValue) => (int)(originalValue * Screen.Density);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToZebble(float scaledValue) => (int)(scaledValue / Screen.Density);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point ToDevice(Point zebblePoint) => new Point(ToDevice(zebblePoint.X), ToDevice(zebblePoint.Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point ToZebble(Point devicePoint) => new Point(ToZebble(devicePoint.X), ToZebble(devicePoint.Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Size ToDevice(Size zebbleSize) => new Size(ToDevice(zebbleSize.Width), ToDevice(zebbleSize.Height));
    }
}
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
        public static Point ToDevice(Point point) => new(ToDevice(point.X), ToDevice(point.Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point ToZebble(Point point) => new(ToZebble(point.X), ToZebble(point.Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Size ToDevice(Size size) => new(ToDevice(size.Width), ToDevice(size.Height));
    }
}
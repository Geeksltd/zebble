using System.Runtime;

namespace System
{
    public class SuspendGC : IDisposable
    {
        static SuspendGC Current;
        static readonly object SyncLock = new();
        readonly GCLatencyMode Original;

        SuspendGC()
        {
            Original = GCSettings.LatencyMode;
#if WINUI
            GCSettings.LatencyMode = GCLatencyMode.LowLatency;
#else
            GCSettings.LatencyMode = GCLatencyMode.NoGCRegion;
#endif
        }

        public static SuspendGC Start()
        {
#if MAUI_IOS || MAUI_ANDROID
            return null;
#else
            lock (SyncLock)
            {
                if (Current != null) return null;
                return Current = new SuspendGC();
            }
#endif
        }

        public void Dispose() => GCSettings.LatencyMode = Original;
    }
}
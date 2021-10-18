using System.Runtime;

namespace System
{
    public class SuspendGC : IDisposable
    {
        static SuspendGC Current;
        static object SyncLock = new object();
        GCLatencyMode Original;

        SuspendGC()
        {
            Original = GCSettings.LatencyMode;
#if UWP
            GCSettings.LatencyMode = GCLatencyMode.LowLatency;
#else
            GCSettings.LatencyMode = GCLatencyMode.NoGCRegion;
#endif
        }

        public static SuspendGC Start()
        {
            lock (SyncLock)
            {
                if (Current != null) return null;
                return Current = new SuspendGC();
            }
        }

        public void Dispose() => GCSettings.LatencyMode = Original;
    }
}
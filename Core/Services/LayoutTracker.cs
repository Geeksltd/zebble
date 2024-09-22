namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Services;
    using Olive;

    namespace Services
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public interface ITrackable
        {
            View View { get; }
            string Type { get; }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public class LayoutTracker
        {
            static readonly List<string> Tracking = new();

            public static void StartTracking(params ITrackable[] trackables) => Tracking.AddRange(trackables.Select(GetTrackingRef));

            static string GetTrackingRef(ITrackable trackable) => trackable.Type + trackable.View?.CssReference;

            public static void Track(ITrackable trackable, object value, [CallerMemberName] string caller = null)
            {
#if WINUI
                if (!UIRuntime.IsDevMode) return;
                if (Tracking.None()) return;
                if (Tracking.Lacks(GetTrackingRef(trackable))) return;

                Log.For<LayoutTracker>().Warning(trackable.Type + " → " + caller + "(" + value + ")    " +
                                                 trackable.View?.GetFullPath());

                Log.For<LayoutTracker>().Debug(GetTrackingTrace());
#endif
            }

            static string GetTrackingTrace()
            {
                return Environment.StackTrace.ToLines().Trim().Select(x => x.TrimStart("at "))
                        .Except(x => x.StartsWithAny("System.", "Zebble.Services.LayoutTracker.", "Zebble.Length.", "Zebble.StylesheetPropertyExtensions", "Zebble.NativeImpl.DeviceThread", "Zebble.NativeImpl.DeviceUIThread.", "Zebble.AsyncEventHandler", "Zebble.View.",
                        "Zebble.AbstractAsyncEvent", "Zebble.AsyncEvent"))
                        .TakeWhile(x => !x.StartsWith("Zebble.Services.CssEngine."))
                        .ToLinesString();
            }
        }
    }

    partial class Length : ITrackable
    {
        View ITrackable.View => Owner;
        string ITrackable.Type => Type.ToString();
    }

    partial class Stylesheet : ITrackable
    {
        View ITrackable.View => Owner;
    }
}
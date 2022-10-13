namespace Zebble
{
    using System.Collections.Generic;
    using System.Linq;
    using Zebble.Services;
    using Olive;

    partial class BatchUIChangeContext
    {
        const int MAX_DEPENDENCY_NESTING = 50;

        internal List<Length> CascadingLengths = new();
        internal ConcurrentList<StyleChangeTracker> ChangeTrackers = new();

        internal void ApplyChanges()
        {
            foreach (var tracker in ChangeTrackers)
                tracker.Apply();

            ChangeTrackers.Clear();
        }

        internal void TrackCascade(Length length)
        {
            if (Device.App.ExitingWithError) return;
            CascadingLengths.Add(length);

            if (CascadingLengths.Count(x => x == length) < MAX_DEPENDENCY_NESTING) return;

            // Infinite loop

            Log.For(this).Error("Detected a cyclic dependency in the layout settings.");
            Log.For(this).Debug("Length: " + length);
            Log.For(this).Debug("==========================");
            Log.For(this).Warning("The CSS rules currently applied to the owner of the above length:");
            Log.For(this).Debug(CssEngine.Diagnose(length.Owner));
            Log.For(this).Debug("--------------------------------------------------------------------");

            Log.For(this).Warning("The length is effectively dependant on:");
            var toShowCss = new List<View>();

            Log.For(this).Debug("");
            Log.For(this).Debug("");
            Log.For(this).Debug("-------------------------------------------");
            Log.For(this).Debug("### CSS RULES OF THE DEPENDANT ON VIEWS ###");
            Log.For(this).Debug("-------------------------------------------");

            foreach (var item in toShowCss.Distinct())
            {
                Log.For(this).Warning("The CSS rules for " + item.GetFullPath());
                Log.For(this).Debug(CssEngine.Diagnose(item));
                Log.For(this).Debug("........................");
                Log.For(this).Debug("");
            }

#if UWP
            Device.App.ExitWithError("Detected cyclic dependency in layout settings. See the output log.");
#endif
        }
    }
}
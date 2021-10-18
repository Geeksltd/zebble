namespace Zebble.UWP
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using controls = Windows.UI.Xaml.Controls;
    using ScrollViewRef = System.WeakReference<ScrollView>;
    using UWPScrollViewRef = System.WeakReference<UWPScrollView>;
    using xaml = Windows.UI.Xaml;
    using Olive;

    partial class UWPScrollView
    {
        xaml.Input.ManipulationDeltaRoutedEventArgs RunningInnertia;

        internal static Dictionary<ScrollViewRef, UWPScrollViewRef> Mappings =
            new Dictionary<ScrollViewRef, UWPScrollViewRef>();

        public bool SupportsChildPanning;

        static UWPScrollView GetFor(ScrollView scrollView)
        {
            return Mappings
                 .Where(x => x.Key.GetTargetOrDefault() == scrollView)
                     .Select(x => x.Value.GetTargetOrDefault())
                     .ExceptNull().FirstOrDefault();
        }

        internal static void EnableManual(ScrollView view) => GetFor(view)?.EnableManualMode();

        void EnableManualMode()
        {
            if (SupportsChildPanning) return;
            else SupportsChildPanning = true;

            Result.HorizontalScrollMode = controls.ScrollMode.Disabled;
            Result.VerticalScrollMode = controls.ScrollMode.Disabled;

            Result.PointerWheelChanged += Result_PointerWheelChanged;

            Result.ManipulationMode = xaml.Input.ManipulationModes.All;

            Result.ManipulationDelta += Result_ManipulationDelta;
            Result.ManipulationStarting += Result_ManipulationStarting;
        }

        void Result_ManipulationStarting(object sender, xaml.Input.ManipulationStartingRoutedEventArgs args)
        {
            var native = Refresher?.Native();
            if (native != null)
                native.Margin = new xaml.Thickness { Left = (float)Result.Width / 2 - 25 };

            RunningInnertia?.Complete();
            RunningInnertia = null;
        }

        void Result_ManipulationDelta(object sender, xaml.Input.ManipulationDeltaRoutedEventArgs args)
        {
            if (args.IsInertial)
            {
                RunningInnertia = args;

                if (View.Refresh.Enabled && Result.VerticalOffset < RefresherHeight)
                {
                    args.Complete();
                    AutoMoveTo(RefresherHeight);
                    return;
                }
            }

            var change = args.Delta.Translation;
            var totalChange = args.Cumulative.Translation;

            if (View.Direction == RepeatDirection.Horizontal && Math.Abs(totalChange.X) > Math.Abs(totalChange.Y))
                ApplyDelta(change.X);

            if (View.Direction == RepeatDirection.Vertical && Math.Abs(totalChange.Y) > Math.Abs(totalChange.X))
                ApplyDelta(change.Y);

            args.Handled = true;
        }

        void Result_PointerWheelChanged(object sender, xaml.Input.PointerRoutedEventArgs args)
        {
            if (!View.EnableScrolling) return;

            var delta = args.GetCurrentPoint(Result).Properties.MouseWheelDelta;

            if (View.Refresh.Enabled && delta > 0 && View.Direction == RepeatDirection.Vertical)
            {
                if (Result.VerticalOffset - delta < RefresherHeight) AutoMoveTo(RefresherHeight);
                else ApplyDelta(delta);
            }
            else ApplyDelta(delta);

            args.Handled = true;
        }

        void ApplyDelta(double delta)
        {
            if (View.Direction == RepeatDirection.Horizontal)
                AutoMoveTo(horizontal: Result.HorizontalOffset + delta, animate: false);
            else
                AutoMoveTo(vertical: Result.VerticalOffset - delta, animate: false);
        }
    }
}
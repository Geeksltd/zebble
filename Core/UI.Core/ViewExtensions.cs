namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Olive;

    public static partial class ViewExtensions
    {
        /// <summary>
        /// Gets all children which are of the specified type whether or not they are ignored.
        /// </summary>
        public static IEnumerable<T> AllChildren<T>(this View @this) where T : View => @this.AllChildren.OfType<T>();

        /// <summary>
        /// Will return all sibling of this view  which are of the specified type whether or not they are Ignored.
        /// </summary>
        public static IEnumerable<T> AllSiblings<T>(this View @this) => @this.AllSiblings.OfType<T>();

        /// <summary>
        /// Gets the non-ignored children which are of the specified type.
        /// </summary>
        public static IEnumerable<T> CurrentChildren<T>(this View @this) where T : View => @this.CurrentChildren.OfType<T>();

        /// <summary>
        /// Will return the non-ignored sibling of this view  which are of the specified type.
        /// </summary>
        public static IEnumerable<T> CurrentSiblings<T>(this View @this) => @this.CurrentSiblings.OfType<T>();

        internal static float VerticalPaddingAndBorder(this View view)
        {
            if (view is null) return 0;
            return view.Padding.Vertical() + view.Effective.BorderTotalVertical();
        }

        internal static float HorizontalPaddingAndBorder(this View view)
        {
            if (view is null) return 0;
            return view.Padding.Horizontal() + view.Effective.BorderTotalHorizontal();
        }

        public static Task<TView> MoveTo<TView>(this TView @this, View newParent) where TView : View
        {
            return @this.MoveTo(newParent, newParent?.AllChildren.Count ?? 0);
        }

        public static bool IsAnyOf(this View @this, params View[] options) => options.Contains(@this);

        public static bool IsAnyOf(this View @this, IEnumerable<View> options) => options.Contains(@this);

        public static async Task<TView> MoveTo<TView>(this TView @this, View newParent, int at) where TView : View
        {
            if (newParent is null) throw new ArgumentNullException(nameof(newParent));

            if (UIRuntime.IsDevMode)
            {
                if (@this.IsRendered() && !newParent.IsRendered())
                    throw new InvalidOperationException("This view is already rendered and cannot be moved to a parent view which is not rendered.");
            }

            if (@this.IsRendered())
                UIWorkBatch.Publish(@this, "[REMOVE]", null);

            @this.parent.AllChildren?.Remove(@this);
            await newParent.AddAt(at, @this);

            return @this;
        }

#if UWP
        public static Windows.UI.Xaml.FrameworkElement Native(this View view) => (Windows.UI.Xaml.FrameworkElement)view.Native;

#elif ANDROID
        public static Android.Views.View Native(this View view) => (Android.Views.View)view.Native;

#elif IOS
        public static UIKit.UIView Native(this View view) => (UIKit.UIView)view.Native;
#endif

        /// <summary>
        /// Removes this view from its parent. It's forgiving if this, and its parent, are null.
        /// </summary>
        public static Task RemoveSelf(this View view) => view?.parent?.Remove(view) ?? Task.CompletedTask;

        /// <summary>Euclidean distance for two points, or null if either of them is null.</summary>
        public static double? DistanceTo(this Point? point, Point? another)
        {
            if (point == null || another is null) return null;

            var xDiff = point.Value.X - another.Value.X;
            var yDiff = point.Value.Y - another.Value.Y;

            return Math.Sqrt(xDiff.ToThePowerOf(2) + yDiff.ToThePowerOf(2));
        }

        /// <summary>
        /// A shortkey to UIWorkBatch.RunSync(...)
        /// </summary>
        public static void BatchUpdate<T>(this T @this, Action<T> change) where T : View
        {
            if (@this != null) UIWorkBatch.RunSync(() => change(@this));
        }

        public static Task SetFromNavParam<TValue, TMember>(this Bindable<TValue> @this, Expression<Func<TValue, TMember>> viewModelProperty,
       string parameterName, AsyncEvent<RevisitingEventArgs> reloadOn = null)
        {
            return SetFrom(@this, viewModelProperty, () => Nav.Param<TMember>(parameterName), reloadOn);
        }

        public static Task SetFrom<TValue, TMember>(this Bindable<TValue> @this, Expression<Func<TValue, TMember>> viewModelProperty,
            Func<TMember> provider, AsyncEvent<RevisitingEventArgs> reloadOn)
        {
            return SetFrom(@this, viewModelProperty, () => Task.FromResult(provider()), reloadOn);
        }

        public static async Task SetFrom<TValue, TMember>(this Bindable<TValue> @this, Expression<Func<TValue, TMember>> viewModelProperty,
            Func<Task<TMember>> provider, AsyncEvent<RevisitingEventArgs> reloadOn = null)
        {
            var binding = new Bindable<TValue>.ViewModelMemberBinding<TMember>(@this, viewModelProperty);

            async Task refresh() => await binding.Update(await provider());

            await refresh();

            reloadOn?.Handle(() => refresh());
        }

        /// <summary>
        /// Assuming this point is on a child view, it returns the relative point for the parent, which
        /// is basically the location of the child within the parent, plus the point.
        /// </summary>
        internal static Point OnParentOf(this Point @this, View child) => @this.Add(child.ActualX, child.ActualY);
    }
}
namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;
    using Olive;

    abstract class StyleChangeTracker
    {
        internal abstract void Apply();
    }

    class StyleChangeTracker<T> : StyleChangeTracker
    {
        readonly T OriginalValue;
        internal T NewValue;
        internal Action Cascader;
        internal bool IsDone;

        public StyleChangeTracker(T original) => OriginalValue = original;

        internal override void Apply()
        {
            try
            {
                if (Cascader is null) return;
                if (EqualityComparer<T>.Default.Equals(OriginalValue, NewValue)) return;
                Cascader();
            }
            finally
            {
                IsDone = true;
            }
        }
    }

    partial class Stylesheet
    {
        StyleChangeTracker<string> BackgroundImagePathChangeTracker;
        StyleChangeTracker<float?> OpacityChangeTracker;
        StyleChangeTracker<Alignment?> BackgroundImageAlignmentChangeTracker;
        StyleChangeTracker<Stretch?> BackgroundImageStretchChangeTracker;
        StyleChangeTracker<Color> TextColorChangeTracker;
        StyleChangeTracker<bool?> WrapTextChangeTracker;

        [EscapeGCop("Special design for performance.")]
        void Change<T>(ref StyleChangeTracker<T> tracker, ref T styleField, T newValue, Action cascader, Action afterFieldSet = null)
        {
            var context = Owner.StyleApplyingContext;

            if (context == null || !IsCss)
            {
                // Immediate mode:
                styleField = newValue;
                afterFieldSet?.Invoke();
                cascader?.Invoke();
            }
            else
            {
                if (tracker == null || tracker.IsDone)
                {
                    tracker = new StyleChangeTracker<T>(styleField) { Cascader = cascader };
                    context.ChangeTrackers.Add(tracker);
                }

                styleField = newValue;
                afterFieldSet?.Invoke();
                tracker.NewValue = newValue;
            }
        }

        /// <summary>
        /// Provides a mechanism to apply a temporary visual style change on a view without ruining its Css and Style settings after the temporary change is reversed.
        /// </summary>
        public class State<T> : IDisposable
        {
            readonly T Style, Css;
            readonly View View;
            readonly PropertyInfo Property;

            internal State(View view, PropertyInfo property)
            {
                View = view;
                Property = property;
                Style = (T)property.GetValue(view.Style);
                Css = (T)property.GetValue(view.Css);
            }

            public void Reverse()
            {
                Property.SetValue(View.Css, Css);
                Property.SetValue(View.Style, Style);
            }

            void IDisposable.Dispose() => Reverse();
        }

        /// <summary>
        /// Provides a mechanism to apply a temporary visual style change on a view without ruining its Css and Style settings after the temporary change is reversed.
        /// </summary>
        public static State<T> Preserve<T>(View view, Expression<Func<Stylesheet, T>> member)
        {
            var prop = (member.Body as MemberExpression).Member as PropertyInfo;
            return new State<T>(view, prop);
        }
    }
}
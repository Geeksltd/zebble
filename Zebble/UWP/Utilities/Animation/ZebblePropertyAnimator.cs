namespace Zebble
{
    using System;
    using Windows.UI.Xaml.Media.Animation;
    using xaml = Windows.UI.Xaml;

    internal class ZebblePropertyAnimator
    {
        public string propertyPath;
        public Timeline Timeline;
        xaml.DependencyObject target;

        public override string ToString() => PropertyPath;

        public xaml.DependencyObject Target
        {
            get => target;
            set => Storyboard.SetTarget(Timeline, target = value);
        }

        public string PropertyPath
        {
            get => propertyPath;
            set => Storyboard.SetTargetProperty(Timeline, propertyPath = value);
        }
    }

    internal class ZebblePropertyAnimator<TValue> : ZebblePropertyAnimator
    {
        public Func<TValue> FromProvider, ToProvider;
        public TValue From, To;
        public override string ToString() => base.ToString() + "    " + From + " -> " + To;
    }
}
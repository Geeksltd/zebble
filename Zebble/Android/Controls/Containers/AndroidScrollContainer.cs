namespace Zebble.AndroidOS
{
    using Android.Views;
    using Android.Widget;

    public class AndroidScrollContainer : AndroidGestureView<AndroidLinearContainer>
    {
        public AndroidScrollContainer(Zebble.ScrollView view) : base(view)
        {
            LayoutContainer = new AndroidLinearContainer(view)
            {
                Orientation = view.Direction == RepeatDirection.Vertical ? Orientation.Vertical : Orientation.Horizontal
            };

            base.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            base.AddView(LayoutContainer);
        }

        internal AndroidLinearContainer GetEffectiveLayoutContainer() => LayoutContainer;

        public override void AddView(View child)
        {
            LayoutContainer.AddView(child);
        }
    }
}
namespace Zebble.AndroidOS
{
    class ScrollViewFactory
    {
        public static Android.Views.View Render(ScrollView scrollView)
        {
            if (scrollView.EnableZooming)
            {
                if (scrollView.Direction == RepeatDirection.Vertical) return new VerticalZoomableScrollView(scrollView);
                else return new HorizontalZoomableScrollView(scrollView);
            }

            if (scrollView.Direction == RepeatDirection.Vertical) return new SimpleVerticalScrollView(scrollView);
            else return new SimpleHorizontalScrollView(scrollView);
        }
    }
}
namespace Zebble.AndroidOS
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Olive;

    public class HitTester
    {
        const int BiggestError = 20;
        Point TouchPoint;
        public HitTester(Point point) => TouchPoint = point;

        public View FindHandler()
        {
            var allHandlers = GetHandlers();

            foreach (var errorMargin in new[] { 0, 2, 3, 6, 10, 15, BiggestError })
                foreach (var handler in allHandlers)
                    if (handler.IsInside(TouchPoint, errorMargin, checkParent: true))
                        return handler.View;

            return null;
        }

        Item[] GetHandlers()
        {
            return GetChildrenItems(View.Root, null).OrderByDescending(x => x.AbsoluteZIndex).ToArray();
        }

        IEnumerable<Item> GetChildrenItems(View parent, Item parentItem)
        {
            foreach (var child in parent.CurrentChildren)
            {
                if (child is Page)
                    if (child != Nav.CurrentPage && child.parent != Nav.CurrentPage)
                        continue;

                if (!child.IsEffectivelyVisible(includingOpacity: true))
                    continue;

                var item = new Item(child, parentItem);
                if (!item.IsInside(TouchPoint, errorMargin: BiggestError, checkParent: false))
                    continue;

                item.CalculateAbsoluteZIndex();

                foreach (var childItem in GetChildrenItems(child, item))
                    yield return childItem;

                yield return item;
            }
        }

        public class Item
        {
            public View View;
            public Point TopLeft;
            public Size Size;
            public Point BottomRight;
            public string AbsoluteZIndex;
            public Item Parent;

            public override string ToString() => TopLeft + " to " + BottomRight + " -> " + View.GetFullPath();

            public Item(View view, Item parent)
            {
                View = view;

                float left = view.ActualX;
                float top = view.ActualY;

                Parent = parent;
                if (parent != null)
                {
                    left += parent.TopLeft.X;
                    top += parent.TopLeft.Y;
                    if (parent.View is ScrollView scroll)
                    {
                        left -= scroll.ScrollX;
                        top -= scroll.ScrollY;
                    }
                }

                TopLeft = new Point(left, top);
                Size = new Size(view.ActualWidth, view.ActualHeight);

                BottomRight = new Point(TopLeft.X + Size.Width, TopLeft.Y + Size.Height);
            }

            public void CalculateAbsoluteZIndex()
            {
                var zIndexParts = new List<string>();
                var item = View.Native();
                var parent = item?.Parent;

                while (parent != null)
                {
                    if (item != null && parent is Android.Views.ViewGroup group)
                    {
                        var index = group.IndexOfChild(item);

                        var z = index;

                        if (Device.OS.IsAtLeast(Android.OS.BuildVersionCodes.Lollipop))
                            try { z = (int)item.GetZ(); } catch { }

                        zIndexParts.Insert(0, z.ToString().PadLeft(2, '0') + "." + index);
                    }

                    // Parent may be disposed during this
                    if (parent != null)
                    {
                        item = parent as Android.Views.View;
                        parent = parent.Parent;
                    }
                }

                AbsoluteZIndex = zIndexParts.ToString(">").Replace("-", ".");
            }

            public bool IsInside(Point point, float errorMargin, bool checkParent)
            {
                if (point.X < TopLeft.X - errorMargin) return false;
                if (point.Y < TopLeft.Y - errorMargin) return false;
                if (point.X > BottomRight.X + errorMargin) return false;
                if (point.Y > BottomRight.Y + errorMargin) return false;

                if (checkParent && Parent != null)
                    if (Parent.IsInside(point, errorMargin, checkParent) == false) return false;

                return true;
            }
        }
    }
}
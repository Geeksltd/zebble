namespace Zebble
{
    using System.Linq;

    partial class Stack
    {
        readonly object ArrangeLock = new();
        View LastManagedChild;

        void PlaceNewVerticalChild(View child, int index)
        {
            if (IsDisposing) return;
            if (AllChildren.Any(x => x.ignored && !x.absolute)) RearrangeItemsVertically();
            else if (child == ManagedChildren.LastOrDefault())
            {
                var previousItem = GetManagedItemBefore(index);
                PlaceVerticallyAfter(child, previousItem);
                BindHeightToLastManagedChild();
            }
            else RearrangeItemsVertically();
        }

        void BindHeightToLastManagedChild()
        {
            if (Height.AutoOption != Length.AutoStrategy.Content) return;

            var previousLastChild = LastManagedChild;
            var newLastChild = ManagedChildren.LastOrDefault();
            if (previousLastChild == newLastChild) return;

            if (previousLastChild != null)
            {
                // Unbind from last managed child
                Height.RemoveUpdateDependency(previousLastChild.Height.Changed);
                Height.RemoveUpdateDependency(previousLastChild.Margin.Bottom.Changed);
                Height.RemoveUpdateDependency(previousLastChild.Y.Changed);
            }

            if (newLastChild != null)
            {
                LastManagedChild = newLastChild;
                Height.UpdateOn(newLastChild.Height.Changed, newLastChild.Margin.Bottom.Changed, newLastChild.Y.Changed);

                if (IsShown)
                    Height.Update();
            }
        }

        void RearrangeItemsVertically()
        {
            lock (ArrangeLock)
            {
                var children = ManagedChildren.ToArray();

                View previous = null;
                foreach (var item in children)
                {
                    PlaceVerticallyAfter(item, previous);
                    previous = item;
                }

                BindHeightToLastManagedChild();
            }
        }

        void PlaceVerticallyAfter(View child, View previousItem)
        {
            if (previousItem is null || child == previousItem)
            {
                child.Y.BindTo(Padding.Top, child.Margin.Top, (x, y) => x + y);
            }
            else
            {
                child.Y.BindTo(previousItem.Y, previousItem.Height, previousItem.Margin.Bottom, child.Margin.Top,
                    (y, h, mb, ct) => previousItem.ActualY /* It's not necessarily the same as Y */ + h + mb + ct);
            }
        }
    }
}
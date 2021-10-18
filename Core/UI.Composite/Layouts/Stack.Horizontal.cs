namespace Zebble
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    partial class Stack
    {
        internal HorizontalAlignment horizontalAlignment = HorizontalAlignment.Left;

        public HorizontalAlignment HorizontalAlignment
        {
            get => horizontalAlignment;
            set => SetHorizontalAlignment(value).GetAwaiter();
        }

        internal async Task SetHorizontalAlignment(HorizontalAlignment value)
        {
            if (horizontalAlignment == value) return;
            horizontalAlignment = value;

            if (direction == RepeatDirection.Vertical)
            {
                Log.For(this).Debug("Remove redundant HorizontalAlignment for vertical stack: " + GetFullPath());
                return;
            }

            if (AllChildren.Any()) await ReAddAllChildren();

            if (value == HorizontalAlignment.Center)
                Width.Changed.HandleWith(RearrangeItemsHorizontally);
        }

        void PlaceNewHorizontalChildren(params View[] children)
        {
            if (IsDisposing) return;
            LinkHorizontalObjectsWidths(children);

            children.Do(x => x.IgnoredChanged.HandleWith(RearrangeItemsHorizontally));

            if (HorizontalAlignment == HorizontalAlignment.Left &&
                children.IsSingle() && children[0] == AllChildren.Last() &&
                AllChildren.None(x => x.ignored))
            {
                PlaceHorizontallyAfter(children[0], GetManagedItemBefore(AllChildren.IndexOf(children[0])));
            }
            else RearrangeItemsHorizontally();

            foreach (var child in children)
                Height.UpdateOn(child.Height.Changed, child.Margin.Top.Changed,
                    child.Margin.Bottom.Changed, child.IgnoredChanged);

            Height.Update();
        }

        void LinkHorizontalObjectsWidths(View[] newChildren)
        {
            var previousSiblings = ManagedChildren.Except(newChildren)
                .Where(x => x.Width.AutoOption == Length.AutoStrategy.Container)
                .ToList();

            foreach (var c in previousSiblings)
            {
                foreach (var newChild in newChildren)
                    // Auto width siblings' width depends on the new ones:
                    c.Width.UpdateOn(newChild.Width.Changed, newChild.Margin.Left.Changed,
                        newChild.Margin.Right.Changed, newChild.IgnoredChanged);

                c.Width.Update();
            }

            foreach (var newChild in newChildren.Where(x => x.Width.AutoOption == Length.AutoStrategy.Container))
            {
                // My width depends on the siblings
                foreach (var sibling in ManagedChildren.Except(newChild))
                    newChild.Width.UpdateOn(sibling.Width.Changed, sibling.Margin.Left.Changed, sibling.Margin.Right.Changed, sibling.IgnoredChanged);
            }

            foreach (var c in ManagedChildren.Where(x => x.Width.AutoOption == Length.AutoStrategy.Container))
                c.Width.Update();
        }

        void PlaceHorizontallyAfter(View child, View previousItem)
        {
            if (previousItem is null)
            {
                child.X.BindTo(Padding.Left, child.Margin.Left, (p, m) => p + m);
            }
            else
            {
                child.X.BindTo(previousItem.X, previousItem.Width, previousItem.Margin.Right, child.Margin.Left,
                    (x, w, m, l) => x + w + m + l);
            }
        }

        void PlaceHorizontallyBefore(View child, View nextItem)
        {
            if (nextItem is null)
            {
                child.X.BindTo(Width, child.Width, Padding.Right, child.Margin.Right, (p, c, pr, cmr) => p - c - pr - cmr);
            }
            else
            {
                child.X.BindTo(nextItem.X, child.Width, nextItem.Margin.Left, child.Margin.Right,
                    (x, w, ml, mr) => x - w - ml - mr);
            }
        }

        /// <summary>Sets the X of all items. It ignores their Width.</summary>
        void RearrangeItemsHorizontally()
        {
            lock (ArrangeLock)
            {
                var children = ManagedChildren.ToArray();

                if (HorizontalAlignment == HorizontalAlignment.Left)
                {
                    View previous = null;
                    foreach (var item in children)
                    {
                        PlaceHorizontallyAfter(item, previous);
                        previous = item;
                    }
                }
                else if (HorizontalAlignment == HorizontalAlignment.Right)
                {
                    children = children.Reverse().ToArray();

                    View next = null;
                    foreach (var item in children)
                    {
                        PlaceHorizontallyBefore(item, next);
                        next = item;
                    }
                }
                else if (horizontalAlignment == HorizontalAlignment.Center)
                {
                    // The X values should not be attached to each other. Everyone should depend on the Width of everyone.

                    foreach (var item in children)
                    {
                        item.X.BindTo(children.Select(x => x.Width).Concat(Width).ToArray(), all =>
                        {
                            var pushRight = (Width.currentValue - children.Sum(x => x.CalculateTotalWidth()) -
                            this.HorizontalPaddingAndBorder()) / 2;

                            var before = children.GetElementsBefore(item).Sum(x => x.CalculateTotalWidth());
                            // Push right:

                            return pushRight + before + item.Margin.Left();
                        })
                            .UpdateOn(
                                     children.Select(x => x.Margin.Left.Changed)
                                     .Concat(children.Select(x => x.Margin.Right.Changed)).ToArray());
                    }
                }
            }
        }
    }
}
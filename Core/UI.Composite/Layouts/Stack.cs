namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    public partial class Stack : Canvas, IAutoContentHeightProvider, IAutoContentWidthProvider
    {
        RepeatDirection direction = RepeatDirection.Vertical;
        readonly AsyncEvent AutoContentHeightChanged = new();
        readonly AsyncEvent AutoContentWidthChanged = new();

        public Stack() : this(RepeatDirection.Vertical) { }
        public Stack(RepeatDirection direction) : base() => Direction = direction;

        public RepeatDirection Direction
        {
            get => direction;
            set => SetDirection(value).GetAwaiter();
        }

        internal async Task SetDirection(RepeatDirection value)
        {
            if (direction == value) return;
            direction = value;

            if (AllChildren.Any()) await ReAddAllChildren();
        }

        /// <summary>
        /// Gets the child views which are not ignored or absolute (in which case this stack will manage their X, Y).
        /// </summary>
        public IEnumerable<View> ManagedChildren => AllChildren.Where(x => !x.absolute && !x.ignored);

        async Task ReAddAllChildren()
        {
            var children = AllChildren.ToArray();

            foreach (var c in children)
            {
                // Remove previous bindings:
                c.X.Clear();
                c.Y.Clear();
            }

            var tempContainer = new Canvas().Absolute().Hide();

            if (children.Any(x => x.IsRendered()))
                await Root.Add(tempContainer, awaitNative: true);

            foreach (var c in children) await c.MoveTo(tempContainer);

            foreach (var c in children) await c.MoveTo(this);

            await tempContainer.RemoveSelf();
        }

        protected override string GetStringSpecifier() => Direction.ToString().Left(1).OnlyWhen(GetType() == typeof(Stack));

        View GetLastItem() => AllChildren.LastOrDefault(x => !x.ignored && !x.absolute);

        View GetManagedItemBefore(int index)
        {
            index--;
            if (index < 0) return null;

            for (var i = index; i >= 0; i--)
            {
                var item = AllChildren.ElementAtOrDefault(i);
                if (item is null) continue; // high concurrency.
                if (item.absolute || item.ignored) continue;
                return item;
            }

            return null;
        }

        public override async Task<TView> AddAt<TView>(int index, TView child, bool awaitNative = false)
        {
            if (IsDisposing) return child;
            await base.AddAt(index, child, awaitNative);

            if (!child.absolute)
            {
                // It's a managed child.
                if (Direction == RepeatDirection.Vertical)
                {
                    Height.UpdateOn(child.IgnoredChanged);
                    child.IgnoredChanged.HandleWith(RearrangeItemsVertically);

                    if (!child.ignored)
                    {
                        PlaceNewVerticalChild(child, index);
                        Height.Update();
                    }
                }
                else
                {
                    PlaceNewHorizontalChildren(child);
                }
            }

            return child;
        }

        public override async Task AddRange<T>(IEnumerable<T> children)
        {
            if (Direction == RepeatDirection.Vertical)
            {
                await base.AddRange(children);
                return;
            }
            else
            {
                foreach (var child in children)
                    await base.AddAt(AllChildren.Count, child);

                PlaceNewHorizontalChildren(children.Except(x => x.absolute).ToArray());
            }
        }

        public override async Task Remove(View child, bool awaitNative = false)
        {
            var isLast = GetLastItem() == child;

            await base.Remove(child, awaitNative);

            if (!child.IsDisposed && child.absolute) return;

            if (Direction == RepeatDirection.Horizontal) RearrangeItemsHorizontally();
            else if (!isLast) RearrangeItemsVertically();
        }

        AsyncEvent IAutoContentHeightProvider.Changed => AutoContentHeightChanged;
        AsyncEvent IAutoContentWidthProvider.Changed => AutoContentWidthChanged;

        protected virtual float CalculateContentAutoHeight()
        {
            if (Direction == RepeatDirection.Horizontal) return Length.FindBiggestChildHeight(this);

            var lastItem = GetLastItem();
            if (lastItem != null)
                return lastItem.ActualBottom + Effective.BorderAndPaddingBottom() + lastItem.Margin.Bottom();

            return this.VerticalPaddingAndBorder();
        }

        [EscapeGCop("GCop bug. Remove me when it's fixed.")]
        float IAutoContentHeightProvider.Calculate() => CalculateContentAutoHeight();

        float IAutoContentWidthProvider.Calculate()
        {
            if (Direction == RepeatDirection.Horizontal)
            {
                return ManagedChildren.Sum(x => x.CalculateTotalWidth()) + this.HorizontalPaddingAndBorder();
            }
            else
            {
                return ManagedChildren.MaxOrDefault(x => x.CalculateTotalWidth()) + this.HorizontalPaddingAndBorder();
            }
        }

        bool IAutoContentHeightProvider.DependsOnChildren() => true;

        bool IAutoContentWidthProvider.DependsOnChildren() => true;

        public override void Dispose()
        {
            AutoContentHeightChanged.Dispose();
            AutoContentWidthChanged.Dispose();
            base.Dispose();
        }

        public void LayoutChildren()
        {
            if (Direction == RepeatDirection.Horizontal)
                RearrangeItemsHorizontally();
            else
                RearrangeItemsVertically();
        }
    }
}
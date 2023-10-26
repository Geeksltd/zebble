namespace Zebble
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using Zebble.Services;
    using Olive;

    public interface IAutoContentWidthProvider { float Calculate(); AsyncEvent Changed { get; } bool DependsOnChildren(); }
    public interface IAutoContentHeightProvider { float Calculate(); AsyncEvent Changed { get; } bool DependsOnChildren(); }

    partial class Length
    {
        bool BoundForAutoHeightContent, BoundForAutoWidthContent;

        public enum AutoStrategy
        {
            /// <summary>The view's width or height should be calculated as the sum of its children.</summary>
            Content,
            /// <summary>The view's width or height should be a proportional amount of the available space in its parent.
            /// For a stack parent, it will depend on the other children.</summary>
            Container
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public AutoStrategy? AutoOption;

        public bool IsAuto() => AutoOption.HasValue;

        /// <summary>
        /// Sets this length to be based on the specified automatic calculation and returns itself back.
        /// </summary>
        public Length Set(AutoStrategy strategy)
        {
            if (AutoOption == strategy || IsDisposing) return this;

            LayoutTracker.Track(this, strategy);

            Clear();
            AutoOption = strategy;
            ApplyAutoStrategy();

            if (Owner.parent is null)
                Owner.ParentSet.Handle(TryAttach);
            else TryAttach();

            void TryAttach()
            {
                if (strategy == AutoStrategy.Content)
                {
                    if (Type == LengthType.Width) WidthContent();
                    else HeightContent();
                }
                else if (Type == LengthType.Width) WidthContainer();
                else HeightContainer();
            }

            void HeightContainer()
            {
                if (Owner.parent is not Stack stack)
                {
                    return;
                }

                void Attach(View sibling)
                {
                    UpdateOn(sibling.IgnoredChanged);
                    UpdateOn(sibling.Height.Changed);
                    UpdateOn(sibling.Margin.Top.Changed);
                    UpdateOn(sibling.Margin.Bottom.Changed);
                }

                UpdateOn(Owner.Margin.Top.Changed);
                UpdateOn(Owner.Margin.Bottom.Changed);

                foreach (var sibling in stack.ManagedChildren.Except(Owner))
                    Attach(sibling);

                stack.ChildAdded.Handle(Attach);
                stack.ChildRemoved.RemoveHandler(Attach);
            }

            void WidthContainer()
            {
                if (Owner.parent is not Stack stack)
                {
                    return;
                }

                void Attach(View sibling)
                {
                    UpdateOn(sibling.IgnoredChanged);
                    UpdateOn(sibling.Width.Changed);
                    UpdateOn(sibling.Margin.Left.Changed);
                    UpdateOn(sibling.Margin.Right.Changed);
                }

                UpdateOn(Owner.Margin.Left.Changed);
                UpdateOn(Owner.Margin.Right.Changed);

                foreach (var sibling in stack.ManagedChildren.Except(Owner))
                    Attach(sibling);

                stack.ChildAdded.Handle(Attach);
                stack.ChildRemoved.RemoveHandler(Attach);
            }

            void HeightContent()
            {
                if (Owner is not Stack stack)
                {
                    return;
                }

                void Attach(View sibling)
                {
                    UpdateOn(sibling.IgnoredChanged);
                    UpdateOn(sibling.Height.Changed);
                    UpdateOn(sibling.Padding.Top.Changed);
                    UpdateOn(sibling.Padding.Bottom.Changed);
                }

                UpdateOn(Owner.Padding.Top.Changed);
                UpdateOn(Owner.Padding.Bottom.Changed);

                foreach (var sibling in stack.ManagedChildren)
                    Attach(sibling);

                stack.ChildAdded.Handle(Attach);
                stack.ChildRemoved.RemoveHandler(Attach);
            }

            void WidthContent()
             {
                if (Owner is not Stack stack)
                {
                    return;
                }

                void Attach(View sibling)
                {
                    UpdateOn(sibling.IgnoredChanged);
                    UpdateOn(sibling.Width.Changed);
                    UpdateOn(sibling.Padding.Left.Changed);
                    UpdateOn(sibling.Padding.Right.Changed);
                }

                UpdateOn(Owner.Padding.Left.Changed);
                UpdateOn(Owner.Padding.Right.Changed);

                foreach (var sibling in stack.ManagedChildren.Except(Owner))
                    Attach(sibling);

                stack.ChildAdded.Handle(Attach);
                stack.ChildRemoved.RemoveHandler(Attach);
            }

            return this;
        }

        void ApplyAutoStrategy()
        {
            if (IsDisposing) return;
            var owner = Owner;
            if (owner is null) return;

            switch (AutoOption)
            {
                case null: return;
                case AutoStrategy.Content:
                    if (Type == LengthType.Width) AutoWidthContent(owner);
                    else if (Type == LengthType.Height) AutoHeightContent(owner);
                    break;
                case AutoStrategy.Container:
                    if (Type == LengthType.Width)
                        ApplyNewValue(AutoWidthByContainer(owner));
                    else if (Type == LengthType.Height)
                        ApplyNewValue(AutoHeightByContainer(owner));
                    break;
                default: throw new NotSupportedException();
            }
        }

        void AutoWidthContent(View owner)
        {
            if (owner is IAutoContentWidthProvider provider)
            {
                if (!BoundForAutoWidthContent)
                {
                    BoundForAutoWidthContent = true;
                    UpdateOn(owner.Padding.Left.Changed, owner.Padding.Right.Changed, owner.HorizontalBorderSizeChanged);
                    UpdateOn(provider.Changed);
                }

                if (owner.Effective is null) return; // Not constructed yet.
                ApplyNewValue(provider.Calculate());
            }
            else
                Log.For(this).Error(owner.GetType().GetProgrammingName() + " does not implement " + nameof(IAutoContentWidthProvider));
        }

        void AutoHeightContent(View owner)
        {
            var provider = owner as IAutoContentHeightProvider;

            if (!BoundForAutoHeightContent)
            {
                BoundForAutoHeightContent = true;
                UpdateOn(owner.Padding.Top.Changed, owner.Padding.Bottom.Changed, owner.VerticalBorderSizeChanged);
                if (provider != null) UpdateOn(provider.Changed);
            }

            if (owner.Effective is null) return; // Not constructed yet.

            ApplyNewValue(provider?.Calculate() ?? FindBiggestChildHeight(owner));
        }

        internal static float FindBiggestChildHeight(View parent)
        {
            return parent.CurrentChildren.MaxOrDefault(x => x.Height.currentValue + x.Margin.Vertical()) + parent.VerticalPaddingAndBorder();
        }

        float AutoWidthByContainer(View owner)
        {
            var parent = owner.parent;

            if (parent is null)
            {
                UpdateOn(owner.ParentSet);
                return 0;
            }

            UpdateOn(parent.Width.Changed, parent.Padding.Left.Changed, parent.Padding.Right.Changed, parent.HorizontalBorderSizeChanged);

            UpdateOn(owner.Margin.Left.Changed, owner.Margin.Right.Changed);

            if (parent is Stack { Direction: RepeatDirection.Horizontal } stack)
            {
                return AutoWidthByHorizontalStackContainer(stack);
            }

            var result = parent.ActualWidth - owner.Margin.Horizontal();
            if (!owner.absolute) result -= parent.HorizontalPaddingAndBorder();

            return result.LimitMin(0);
        }

        float AutoHeightByContainer(View owner)
        {
            var parent = owner.parent;
            if (parent is null)
            {
                owner.ParentSet.Event += Update;
                return 0;
            }

            UpdateOn(parent.Height.Changed, parent.Padding.Top.Changed, parent.Padding.Bottom.Changed, parent.VerticalBorderSizeChanged);

            if (parent is Stack { Direction: RepeatDirection.Vertical } stack)
            {
                return AutoHeightByVerticalStackContainer(stack);
            }

            var result = parent.ActualHeight - owner.Margin.Vertical();
            if (!owner.absolute) result -= parent.VerticalPaddingAndBorder();

            return result.LimitMin(0);
        }

        float AutoHeightByVerticalStackContainer(Stack stack)
        {
            var withSiblings = stack.ManagedChildren.ToArray();

            var demandingSiblings = withSiblings.Where(x => x.Height.AutoOption == AutoStrategy.Container).ToList();
            if (demandingSiblings.None()) return 0; // owner is absolute or ignored.

            // Space already allocated explicitly:
            var availableSpace =
                stack.Height.currentValue
                - withSiblings.Except(demandingSiblings).Sum(c => c.CalculateTotalHeight())
                - demandingSiblings.Sum(x => x.Margin.Vertical())
                - stack.VerticalPaddingAndBorder();

            if (availableSpace <= 0) return 0;

            return availableSpace / demandingSiblings.Count;
        }

        float AutoWidthByHorizontalStackContainer(Stack stack)
        {
            var withSiblings = stack.ManagedChildren.ToArray();

            var demandingSiblings = withSiblings.Where(x => x.Width.AutoOption == AutoStrategy.Container).ToList();
            if (demandingSiblings.None()) return 0;

            // Space already allocated explicitly:
            var availableSpace =
                stack.Width.currentValue
                - withSiblings.Except(demandingSiblings).Sum(c => c.CalculateTotalWidth())
                - demandingSiblings.Sum(x => x.Margin.Horizontal())
                - stack.HorizontalPaddingAndBorder();

            if (availableSpace <= 0) return 0;

            var autoWidthSiblings = demandingSiblings.Count;
            if (stack.horizontalAlignment == HorizontalAlignment.Center)
                autoWidthSiblings += 2;

            return availableSpace / autoWidthSiblings;
        }
    }
}
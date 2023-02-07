namespace Zebble
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Zebble.Services;
    using Olive;

    public partial class Length : IDisposable
    {
        static readonly Length[] EmptyLengths = new Length[0];

        [EditorBrowsable(EditorBrowsableState.Never)]
        public enum LengthType
        {
            Width, Height, X, Y,
            MarginTop, MarginBottom, MarginLeft, MarginRight,
            PaddingTop, PaddingBottom, PaddingLeft, PaddingRight
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public LengthType Type;
        bool IsDisposing;
        internal View Owner;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public float? FixedValue, PercentageValue;
        Func<float> ExpressionValue;
        internal bool IsUnknown = true;

        internal float currentValue;
        float? minLimit, maxLimit;

        public float CurrentValue => currentValue;

        public float? MinLimit { get => minLimit; set { minLimit = value; ApplyLimits(); } }

        public float? MaxLimit { get => maxLimit; set { maxLimit = value; ApplyLimits(); } }

        /// <summary>
        /// Returns the value of this property in a textual format.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string AsText
        {
            get
            {
                if (FixedValue.HasValue) return FixedValue.ToString();
                else if (PercentageValue.HasValue) return PercentageValue + "%";
                else if (AutoOption.HasValue) return AutoOption.ToString();
                else if (ExpressionValue != null) return "[EXPRESSION]";
                else return string.Empty;
            }
            set
            {
                value = value.OrEmpty().Trim();
                if (value.IsEmpty()) Clear();
                else if (value.Is<double>()) Set(value.To<float>());
                else if (value == "[EXPRESSION]") return;
                else if (value.TryParseAs<AutoStrategy>().HasValue) Set(value.To<AutoStrategy>());
                else if (value.EndsWith("%") && value.TrimEnd("%").Is<double>())
                    Set(value.TrimEnd("%").To<float>().Percent());
                else throw new Exception("Cannot interpreset the specified text as a length: " + value);
            }
        }

        internal Length(View owner, LengthType type)
        {
            Owner = owner;
            RaisesBoundsChanged = type == LengthType.Height || type == LengthType.Width || type == LengthType.X || type == LengthType.Y;

            if (UIRuntime.IsDevMode) Changed.SetOwner(this);
            Type = type;

            switch (type)
            {
                case LengthType.X:
                    owner.ParentSet.Event -= AttachToParentPaddingLeftChanged;
                    owner.ParentSet.Event += AttachToParentPaddingLeftChanged;
                    break;
                case LengthType.Y:
                    owner.ParentSet.Event -= AttachToParentPaddingTopChanged;
                    owner.ParentSet.Event += AttachToParentPaddingTopChanged;
                    break;
                case LengthType.Width:
                    LayoutTracker.Track(this, AutoOption = AutoStrategy.Container);
                    UpdateOn(owner.ParentSet);
                    break;
                case LengthType.Height:
                    LayoutTracker.Track(this, AutoOption = AutoStrategy.Content);
                    UpdateOn(owner.Padding.Top.Changed, owner.Padding.Bottom.Changed, owner.VerticalBorderSizeChanged);
                    if (owner is IAutoContentHeightProvider au) UpdateOn(au.Changed);
                    break;
                default: break;
            }
        }

        void AttachToParentPaddingLeftChanged()
        {
            if (Owner?.parent is null) return;
            UpdateOn(Owner.parent.Padding.Left.Changed);
        }

        void AttachToParentPaddingTopChanged()
        {
            if (Owner?.parent is null) return;
            UpdateOn(Owner.parent.Padding.Top.Changed);
        }

        public override string ToString()
        {
            return "(" + Owner?.GetFullPath() + ") ▶ " + Type + " ⇒ " +
                new[] { AutoOption ?.ToStringOrEmpty(),
                PercentageValue.ToString().WithSuffix("%") , FixedValue.ToString(),
                "[Expression]".OnlyWhen(ExpressionValue != null)}.Trim().FirstOrDefault().Or("N/A");
        }

        public string Dependencies => "";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (!(obj is Length)) return false;
            else return this == (Length)obj;
        }

        public override int GetHashCode() => (int)currentValue;

        /// <summary>
        /// Binds the value of this length to the specified expression so it can be updated later on automatically.
        /// This should only be used if you call UpdateOn() after calling this.
        /// </summary>
        public Length BindTo(Func<float> expression) => BindTo(EmptyLengths, x => expression());

        /// <summary>
        /// Binds the value of this length to be always the same as another length (it will cascade upon future changes too) and return itself.
        /// </summary>
        public Length BindTo(Length another) => BindTo(another, x => x);

        /// <summary>
        /// Binds the value of this length to be always based on another length (it will cascade upon future changes too) and return itself.
        /// </summary>
        public Length BindTo(Length another, Func<float, float> expression)
        {
            return BindTo(new[] { another }, x => expression(x[0]));
        }

        /// <summary>
        /// Binds the value of this length to be always based on other length objects (it will cascade upon future changes too)
        /// and return itself.
        /// </summary>
        public Length BindTo(Length l1, Length l2, Func<float, float, float> expression)
        {
            return BindTo(new[] { l1, l2 }, values => expression(values[0], values[1]));
        }

        /// <summary>
        /// Binds the value of this length to be always based on other length objects (it will cascade upon future changes too)
        /// and return itself.
        /// </summary>
        public Length BindTo(Length l1, Length l2, Length l3, Func<float, float, float, float> expression)
        {
            return BindTo(new[] { l1, l2, l3 }, values => expression(values[0], values[1], values[2]));
        }

        /// <summary>
        /// Binds the value of this length to be always based on other length objects (it will cascade upon future changes too)
        /// and return itself.
        /// </summary>
        public Length BindTo(Length l1, Length l2, Length l3, Length l4, Func<float, float, float, float, float> expression)
        {
            return BindTo(new[] { l1, l2, l3, l4 }, values => expression(values[0], values[1], values[2], values[3]));
        }

        /// <summary>
        /// Binds the value of this length to be always based on other length objects (it will cascade upon future changes too)
        /// and return itself.
        /// </summary>
        public Length BindTo(Length l1, Length l2, Length l3, Length l4, Length l5, Func<float, float, float, float, float, float> expression)
        {
            return BindTo(new[] { l1, l2, l3, l4, l5 }, values => expression(values[0], values[1], values[2], values[3], values[4]));
        }

        /// <summary>
        /// Binds the value of this length to be always based on other length objects (it will cascade upon future changes too)
        /// and return itself.
        /// </summary>
        public Length BindTo(Length[] others, Func<float[], float> expression)
        {
            if (IsDisposing) return this;

            if (UIRuntime.IsDevMode)
            {
                if (others is null) throw new ArgumentNullException(nameof(others));
                if (expression is null) throw new ArgumentNullException(nameof(expression));
                if (others.Contains(this))
                {
                    Log.For(this).Error("You cannot bind a length object to itself: " + this);
                    return this;
                }
            }

            Clear();

            LayoutTracker.Track(this, expression);

            Func<float> expressionValue = () => expression(others.Select(x => x.currentValue).ToArray());
            ExpressionValue = expressionValue;
            ApplyNewValue(expressionValue());

            foreach (var item in others) UpdateOn(item.Changed);

            return this;
        }

        void UpdateNonStackParentHeight()
        {
            var parent = Owner?.parent;
            if (parent == null || parent is Stack) return;
            if (parent.Height.AutoOption != AutoStrategy.Content) return;

            parent.Height.Update();
        }

        readonly Action AllDependencies;

        public void Clear()
        {
            // Remove dependencies

            if (IsDisposing) return;

            IsUnknown = true;
            FixedValue = null;
            AutoOption = null;
            ExpressionValue = null;

            if (Owner != null)
                Owner.ParentSet.Event -= Update;

            if (PercentageValue.HasValue)
            {
                if (Owner != null)
                    Owner.ParentSet.Event -= BindToPercentageOfParent;
                PercentageValue = null;
            }

            if (Type == LengthType.Y || Type == LengthType.Height)
                if (Owner != null)
                    Owner.ParentSet.Event -= UpdateNonStackParentHeight;

            var parent = Owner?.parent;
            if (parent != null)
            {
                if (Type == LengthType.Y) UpdateOn(parent.Padding.Top.Changed);
                else if (Type == LengthType.X) UpdateOn(parent.Padding.Left.Changed);
            }
        }

        /// <summary>
        /// Sets this length to a specified fix point value and returns itself back.
        /// </summary>
        public Length Set(float fixedValue)
        {
            if (FixedValue == fixedValue || IsDisposing) return this;
            LayoutTracker.Track(this, fixedValue);

            Clear();
            FixedValue = fixedValue;
            ApplyNewValue(fixedValue);

            return this;
        }

        void ApplyLimits()
        {
            Update();
            if (FixedValue.HasValue) ApplyNewValue(FixedValue.Value);
        }

        /// <summary>
        /// Sets this length to a specified percentage value and returns itself back.
        /// </summary>
        public Length Set(PercentageLengthRequest percent)
        {
            var value = percent.Percent;
            if (PercentageValue == value || IsDisposing) return this;
            var owner = Owner;
            if (owner is null) return this;

            LayoutTracker.Track(this, percent);

            PercentageValue = value;
            BindToPercentageOfParent();
            owner.ParentSet.Event -= BindToPercentageOfParent;
            owner.ParentSet.Event += BindToPercentageOfParent;

            return this;
        }

        void BindToPercentageOfParent()
        {
            var percent = PercentageValue;
            if (percent is null) return;

            var currentParent = Owner?.parent;

            if (IsDisposing || currentParent is null) return;

            var value = percent.Value * 0.01f;

            Clear();

            PercentageValue = percent; // Clear has removed it.

            switch (Type)
            {
                case LengthType.Height:

                    if (value == 1 && currentParent.VerticalPaddingAndBorder() == 0) { BindTo(currentParent.Height); break; }

                    ExpressionValue = () =>
                    ((currentParent.Height.currentValue - currentParent.VerticalPaddingAndBorder()) * value)
                    .LimitMin(0);
                    UpdateOn(currentParent.Padding.Top.Changed, currentParent.Padding.Bottom.Changed, currentParent.Height.Changed, currentParent.VerticalBorderSizeChanged);
                    break;

                case LengthType.Y:
                    if (value == 1) { BindTo(currentParent.Height); break; }

                    ExpressionValue = () => (currentParent.Height.currentValue - currentParent.VerticalPaddingAndBorder()) * value;
                    UpdateOn(currentParent.Padding.Top.Changed, currentParent.Padding.Bottom.Changed, currentParent.Height.Changed, currentParent.VerticalBorderSizeChanged);
                    break;

                case LengthType.Width:

                    if (value == 1 && currentParent.HorizontalPaddingAndBorder() == 0) { BindTo(currentParent.Width); break; }

                    ExpressionValue = () => ((currentParent.Width.currentValue - currentParent.HorizontalPaddingAndBorder()) * value).LimitMin(0);
                    UpdateOn(currentParent.Padding.Left.Changed, currentParent.Padding.Right.Changed, currentParent.Width.Changed, currentParent.HorizontalBorderSizeChanged);
                    break;

                case LengthType.X:
                    ExpressionValue = () => (currentParent.Width.currentValue - currentParent.HorizontalPaddingAndBorder()) * value;
                    UpdateOn(currentParent.Padding.Left.Changed, currentParent.Padding.Right.Changed, currentParent.Width.Changed, currentParent.HorizontalBorderSizeChanged);
                    break;

                default: throw new NotSupportedException();
            }

            if (ExpressionValue is not null) ApplyNewValue(ExpressionValue());
        }

        /// <summary>
        /// Subscribes to a specified event so it gets re-evaluated after occurrence of that event, and return itself.
        /// This should be called after the primary setter method, i.e. Set(...), SetPercent(...), or BindTo(...) because 
        /// each one of those, will remove all previous UpdateOn attachments.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Length UpdateOn(params AbstractAsyncEvent[] @events)
        {
            if (IsDisposing || @events is null) return this;

            foreach (var @event in @events)
            {
                if (@event is null) continue;
                @event.Event -= Update;
                @event.Event += Update;
            }

            return this;
        }

        /// <summary>
        /// Subscribes to a specified event so it gets re-evaluated after occurrence of that event, and return itself.
        /// This should be called after the primary setter method, i.e. Set(...), SetPercent(...), or BindTo(...) because 
        /// each one of those, will remove all previous UpdateOn attachments.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveUpdateDependency(AbstractAsyncEvent @event)
        {
            if (IsDisposing || @event is null) return;
            @event.Event -= Update;
        }

        /// <summary>
        /// Re-evaluates the value of this length. If changed, it will cascade the change to all its dependants.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update()
        {
            if (ExpressionValue != null) ApplyNewValue(ExpressionValue());
            else if (AutoOption.HasValue)
            {
                if (!IsTooEarlyForAutoValue()) ApplyAutoStrategy();
            }
            else if (PercentageValue.HasValue) BindToPercentageOfParent();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EnsureHeightCalculatedOnce()
        {
            if (currentValue != 0) return;
            if (Type != LengthType.Height) return;
            if (!(Owner is IAutoContentHeightProvider)) return;
            ApplyAutoStrategy();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsTooEarlyForAutoValue()
        {
            var owner = Owner;

            if (currentValue != 0) return false;
            if (owner?.parent != null) return false;

            if (AutoOption == AutoStrategy.Container) return true;

            // OK, it's based on content, and it's not added to a parent yet.

            if (Type == LengthType.Height)
            {
                if ((owner as IAutoContentHeightProvider)?.DependsOnChildren() == false)
                    return false;

                if (owner is Stack stack && stack.AllChildren.IsSingle() && stack.parent != null)
                    return false; // The children are not guaranteed to fire their changed event
            }

            if (Type == LengthType.Width && (owner as IAutoContentWidthProvider)?.DependsOnChildren() == false)
                return false;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ApplyNewValue(float newValue)
        {
            if (UIRuntime.IsDevMode && float.IsInfinity(newValue))
                throw new Exception("Infinity is not a valid value for " + Type + " of: " + Owner?.GetFullPath());

            IsUnknown = false;

            newValue = newValue.LimitMax(maxLimit ?? newValue).LimitMin(minLimit ?? newValue);

            if (newValue.AlmostEquals(currentValue)) return;
            currentValue = newValue;

            CascadeChanged();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            Clear();
            IsDisposing = true;
            Changed?.Dispose();
            Owner = null;
        }
    }
}
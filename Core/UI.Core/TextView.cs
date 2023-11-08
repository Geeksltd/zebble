namespace Zebble
{
    using System;
    using System.Collections.Concurrent;
    using System.ComponentModel;
    using Olive;

    public class TextView : TextControl, IAutoContentWidthProvider
    {
        readonly AsyncEvent AutoContentWidthChanged = new();
        static readonly ConcurrentDictionary<string, float> HeightCache = new();
        static readonly int AutoWrapCheckThreshold = 20 /*char*/ * 14 /*Font size*/ ;
        public readonly AsyncEvent TextChanged = new();
        public readonly AsyncEvent LineHeightChanged = new();
        bool autoSizeWidth;
        float? lineHeight;

        public TextView()
        {
            Height.UpdateOn(Padding.Top.Changed, Padding.Bottom.Changed, VerticalBorderSizeChanged, TextChanged, LineHeightChanged);
        }

        public TextView(string text) : this() => Text = text;

        internal override void RaiseFontChanged()
        {
            if (!Width.IsUnknown && Width.AutoOption == Length.AutoStrategy.Content && text.HasValue())
                AutoContentWidthChanged?.Raise();
            base.RaiseFontChanged();
        }

        protected override Alignment GetDefaultAlignment() => Alignment.Left;

        public override string Text
        {
            get => text;
            set
            {
                if (text == value) return;

                var previouslyWrapped = ShouldWrap();

                text = value;
                TextChanged.Raise();

                var nowWrap = ShouldWrap();

                if (nowWrap || previouslyWrapped)
                    AutoContentHeightChanged?.Raise();

                if (Width.AutoOption == Length.AutoStrategy.Content)
                    AutoContentWidthChanged?.Raise();
            }
        }

        public virtual bool? Wrap
        {
            get => Style.wrapText ?? Css.wrapText;
            set { Style.WrapText = value; AutoContentHeightChanged.Raise(); }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldWrap()
        {
            if (Wrap.HasValue) return Wrap.Value;
            return text.HasValue() && text.Length * Font.EffectiveSize > AutoWrapCheckThreshold;
        }

        [PropertyGroup("Frame")]
        public bool AutoSizeWidth
        {
            get => autoSizeWidth;
            set
            {
                autoSizeWidth = value;
                if (value)
                {
                    Style.Width = new Length.AutoLengthRequest(Length.AutoStrategy.Content);
                    Width.UpdateOn(Padding.Left.Changed, Padding.Right.Changed, HorizontalBorderSizeChanged);
                }
            }
        }

        public float? LineHeight
        {
            get
            {
                if (lineHeight == 0) lineHeight = Font.GetLineHeight();
                return lineHeight;
            }
            set
            {
                if (lineHeight.HasValue && value.HasValue && lineHeight.Value.AlmostEquals(value.Value)) return;
                lineHeight = value;
                LineHeightChanged.Raise();
            }
        }

        public override string ToString() => base.ToString() + " ➔ " + Text.Summarize(20);

        protected override string GetStringSpecifier() => Text;

        protected internal override void OnTextTransformChanged() => TextChanged.Raise();

        float IAutoContentWidthProvider.Calculate() => Font.GetTextWidth(Text) + this.HorizontalPaddingAndBorder();

        protected override float CalculateAutoHeight()
        {
            if (ShouldWrap())
            {
                Height.UpdateOn(Width.Changed);

                if (Width.currentValue == 0) return 0;

                var width = (ActualWidth - this.HorizontalPaddingAndBorder()).LimitMin(0);
                var key = Text + Font + width + "|" + this.VerticalPaddingAndBorder();

                var currentHeight = HeightCache.GetOrAdd(key, k => Font.GetTextHeight(width, Text) + this.VerticalPaddingAndBorder());

                if (LineHeight.HasValue)
                {
                    var linesCount = currentHeight / Font.GetLineHeight();
                    var lineHeight = LineHeight.Value;
                    currentHeight = lineHeight * linesCount;
                }

                return currentHeight;
            }
            else
                return LineHeight ?? Font.GetLineHeight() + this.VerticalPaddingAndBorder();
        }

        AsyncEvent IAutoContentWidthProvider.Changed => AutoContentWidthChanged;

        bool IAutoContentWidthProvider.DependsOnChildren() => false;
    }
}
namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    public abstract class TextControl : View, ITextControl, FormField.IControl, IAutoContentHeightProvider
    {
        const int DEFAULT_FONT_SIZE = 14;
        protected string text;
        protected readonly AsyncEvent AutoContentHeightChanged = new();
        public readonly AsyncEvent TextAlignmentChanged = new();
        public readonly AsyncEvent FontChanged = new();

        protected TextControl() => Css.Font(DEFAULT_FONT_SIZE);

        internal void RaiseTextColorChanged()
        {
            if (IsRendered())
                UIWorkBatch.Publish(this, "TextColor", new TextColorChangedEventArgs(this, TextColor));
        }

        internal void RaiseTextAlignmentChanged() => TextAlignmentChanged.Raise();

        public abstract string Text { get; set; }

        public Alignment TextAlignment
        {
            get => Style.textAlignment ?? Css.textAlignment ?? GetDefaultAlignment();
            set => Style.TextAlignment = value;
        }

        protected abstract Alignment GetDefaultAlignment();

        public Color TextColor
        {
            get => Style.textColor ?? Css.textColor ?? Colors.Black;
            set => Style.TextColor = value;
        }

        /// <summary>
        /// This gives you the current font information, derived from its own styles and CSS.
        /// To change it, use myView.Styles.Font
        /// </summary>
        public IFont Font { get => Effective.Font(); set => Style.Font = (Font)value; }

        internal virtual void RaiseFontChanged()
        {
            if (Height.AutoOption == Length.AutoStrategy.Content)
                AutoContentHeightChanged.Raise();

            if (parent != null || IsRendered()) FontChanged.Raise();
        }

        object FormField.IControl.Value
        {
            get => Text;
            set => Text = value.ToStringOrEmpty();
        }

        public TextTransform TextTransform
        {
            get => Style.textTransform ?? Css.textTransform ?? TextTransform.None;
            set => Style.TextTransform = value;
        }

        internal protected virtual void OnTextTransformChanged() { }

        public string TransformedText => TextTransform.Apply(text);

        AsyncEvent IAutoContentHeightProvider.Changed => AutoContentHeightChanged;

        protected abstract float CalculateAutoHeight();

        float IAutoContentHeightProvider.Calculate() => CalculateAutoHeight();

        public override void Dispose()
        {
            AutoContentHeightChanged?.Dispose();
            TextAlignmentChanged?.Dispose();
            FontChanged?.Dispose();
            base.Dispose();
        }

        bool IAutoContentHeightProvider.DependsOnChildren() => false;

        public override IEnumerable<View> AllDescendents() => Enumerable.Empty<View>();

        public override async Task OnPreRender()
        {
            await base.OnPreRender();

            if (UIRuntime.IsDevMode)
            {
                if (Height.CurrentValue + 1 < Font.GetLineHeight() + this.VerticalPaddingAndBorder())
                {
                    Log.For(this).Warning($"The effective height [{Height.CurrentValue}] is less than " +
                          $"the sum of font line height [{Font.GetLineHeight()}] and " +
                          $"vertical total padding [{this.VerticalPaddingAndBorder()}] " +
                          $"for: {GetType().Name} \"{Text}\"");

                    if (TextAlignment.ToVerticalAlignment() == VerticalAlignment.Middle)
                        Log.For(this).Debug("As its vertically middle-aligned, you can remove the vertical padding.");
                }
            }
        }
    }

    public interface ITextControl { string Text { get; set; } }
}
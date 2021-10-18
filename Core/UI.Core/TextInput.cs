namespace Zebble
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Olive;

    public partial class TextInput : TextControl, FormField.IPlaceHolderControl, IBindableInput
    {
        const int AUTOSIZE_MAX_HEIGHT = 150, AUTOSIZE_MAX_LINE_NUMBER = 1000;
        event InputChanged InputChanged;
        string placeholder;
        Color placeholderColor = Colors.LightGray;
        int lines = 1;
        SpellCheckingType spellChecking;
        AutoCorrectionType autoCorrection;
        AutoCapitalizationType autoCapitalization;
        int autoResizeMaxHeight = AUTOSIZE_MAX_HEIGHT;
        bool autoResize, multiLineAutoResize;
        TextInputAutoResizer AutoResizer;
        public readonly TwoWayBindable<string> Value = new TwoWayBindable<string>();
        public readonly TwoWayBindable<bool> Focused = new TwoWayBindable<bool>();

        public TextMode TextMode { get; set; }
        public KeyboardActionType KeyboardActionType { get; set; }

        /// <summary>Fired when the user is done typing by either clicking enter (or submit, go, etc) button.</summary>
        public readonly AsyncEvent UserTextChangeSubmitted = new AsyncEvent();
        internal readonly AsyncEvent PlaceholderColorChanged = new AsyncEvent();
        public readonly AsyncEvent UserTextChanged = new AsyncEvent();

        internal readonly AsyncEvent AutocapitalizationChanged = new AsyncEvent();
        internal readonly AsyncEvent SpellCheckingChanged = new AsyncEvent();
        internal readonly AsyncEvent AutoCorrectionChanged = new AsyncEvent();
        internal readonly AsyncEvent UserTappedOnReturnKey = new AsyncEvent();

        public TextInput()
        {
            Focused.Changed += SetFocusedCssState;
            Focused.ChangedByInput += SetFocusChanged;
            Value.Changed += SetTextFromValue;
            Value.ChangedByInput += InvokeUserTextChanged;
        }

        void SetFocusChanged() => InputChanged?.Invoke(nameof(Focused));

        event InputChanged IBindableInput.InputChanged { add => InputChanged += value; remove => InputChanged -= value; }

        void SetFocusedCssState() => SetPseudoCssState("focus", Focused.Value).RunInParallel();

        void SetTextFromValue() => text = TextTransform.Apply(Value.Value);

        void InvokeUserTextChanged()
        {
            UserTextChanged?.Raise();
            InputChanged?.Invoke(nameof(Text));
        }

        protected override Alignment GetDefaultAlignment() => Alignment.TopLeft;

        public void Focus()
        {
            if (!Focused.Value) Focused.Set(true);
        }

        public void UnFocus()
        {
            if (Focused.Value) Focused.Set(false);
        }

        public override string Text
        {
            get => text;
            set
            {
                if (text == value) return;
                text = value;
                Value.Set(TransformedText);
            }
        }

        public SpellCheckingType SpellChecking
        {
            get => spellChecking;
            set
            {
                if (value != spellChecking)
                {
                    spellChecking = value;
                    SpellCheckingChanged.Raise();
                }
            }
        }

        /// <summary>
        /// Only needed for iOS. In Android and UWP this can achived by setting SpellChecking.
        /// </summary>
        public AutoCorrectionType AutoCorrection
        {
            get => autoCorrection;
            set
            {
                if (value != autoCorrection)
                {
                    autoCorrection = value;
                    AutoCorrectionChanged.Raise();
                }
            }
        }

        public AutoCapitalizationType AutoCapitalization
        {
            get => autoCapitalization;
            set
            {
                if (autoCapitalization != value)
                {
                    autoCapitalization = value;
                    AutocapitalizationChanged.Raise();
                }
            }
        }

        public string Placeholder
        {
            get => placeholder;
            set
            {
                if (placeholder == value) return;
                placeholder = value;
                UIWorkBatch.Publish(this, "Placeholder", new UIChangedEventArgs<string>(this, value));
            }
        }

        public Color PlaceholderColor
        {
            get => placeholderColor;
            set
            {
                if (placeholderColor == value) return;
                placeholderColor = value;
                PlaceholderColorChanged.Raise();
            }
        }

        public int AutoResizeMaxHeight
        {
            get => autoResizeMaxHeight;
            set
            {
                if (value == autoResizeMaxHeight) return;
                autoResizeMaxHeight = value;
                if (AutoResizer != null) AutoResizer.MaxHeight = value;
            }
        }

        public int AutoResizeMaxLineNumber { get; set; } = AUTOSIZE_MAX_LINE_NUMBER;

        public bool AutoResize
        {
            get => autoResize;
            set
            {
                if (value)
                {
                    Lines = AutoResizeMaxLineNumber;
                    AutoResizer = new TextInputAutoResizer(this, AutoResizeMaxHeight);
                }

                autoResize = value;
            }
        }

        public bool MultiLineAutoResize
        {
            get => multiLineAutoResize;
            set
            {
                multiLineAutoResize = value;

                if (multiLineAutoResize)
                {
                    autoResize = multiLineAutoResize;
                    Lines = AutoResizeMaxLineNumber;
                    AutoResizer = new TextInputAutoResizer(this, AutoResizeMaxHeight);
                }
            }
        }

        public int Lines
        {
            get => lines;
            set
            {
                if (value < 1) throw new Exception("Lines should be 1 or more.");
                lines = value;
                Height.Update();
            }
        }

        protected internal override void OnTextTransformChanged() => Value.Set(TransformedText);

        protected override float CalculateAutoHeight()
        {
            var lineHeight = Font.GetLineHeight();

            if (lines > 1) lineHeight += Font.GetUnwantedExtraTopPadding();

            if (lines > 5) lineHeight += 1; // UWP is being strange.

            return Math.Max(1, lines) * lineHeight + this.VerticalPaddingAndBorder();
        }

        internal TextMode GetEffectiveTextMode()
        {
            if (TextMode != TextMode.Auto) return TextMode;
            if (id.IsEmpty()) return TextMode.GeneralText;

            if (id.Contains("Email", caseSensitive: false)) return TextMode.Email;
            if (id.ContainsAny(new[] { "Phone", "Tel", "Fax", "Mobile" }, caseSensitive: false)) return TextMode.Decimal;
            if (id.Contains("Password", caseSensitive: false)) return TextMode.Password;
            if (id.ContainsAny(new[] { "Url", "Website" }, caseSensitive: false)) return TextMode.Url;
            if (id.ContainsAny(new[] { "Cost", "Price", "Amount", "Total", }, caseSensitive: false)) return TextMode.Decimal;
            if (id.Contains("Name", caseSensitive: false)) return TextMode.PersonName;
            if (id.Contains("Quantity", caseSensitive: false) || id.ToLower().StartsWith("numberof")) return TextMode.Integer;

            return TextMode.GeneralText;
        }

        public override void Dispose()
        {
            Focused.ClearBindings();
            Value.ClearBindings();

            UserTextChangeSubmitted?.Dispose();
            PlaceholderColorChanged?.Dispose();
            UserTextChanged?.Dispose();
            AutoResizer = null;
            InputChanged = null;

            base.Dispose();
        }

        [EscapeGCop("Authorised, to prevent cascading")]
        internal async void FocusOnNextInput()
        {
            await Task.Delay(50); // to prevent cascading

            FormField.IControl nextField = null;

            for (var generation = 1; generation <= 4; generation++)
            {
                var searchNode = WithAllParents().Skip(generation).FirstOrDefault();
                if (searchNode is null) return;

                nextField = searchNode.CurrentDescendants().Except(x => x is TextView || x is ImageView).OfType<FormField.IControl>()
                  .SkipWhile(t => t != this).Skip(1).FirstOrDefault();

                if (nextField != null) break;
            }

            (nextField as TextInput)?.Focus();
        }

        public void AddBinding(Bindable bindable) => UserTextChanged.Handle(() => (bindable as TwoWayBindable<string>).SetByInput(Text));
    }
}
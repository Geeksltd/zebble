namespace Zebble.IOS
{
    using CoreGraphics;
    using Foundation;
    using System;
    using UIKit;
    using Olive;

    public class IosTextBox : UITextField, UIChangeCommand.IHandler
    {
        TextInput View;
        public UIEdgeInsets EdgeInsets { get; set; }

        public IosTextBox(TextInput view) : base(view.GetFrame())
        {
            View = view;
            SetSize();
            Configure();
            HandleEvents();
            Frame = view.GetFrame();

            EditingDidBegin += IosTextBox_EditingDidBegin;
            EditingDidEnd += IosTextBox_EditingDidEnd;
            EditingChanged += IosTextBox_EditingChanged;
            EditingDidEndOnExit += DismissKeyboard;
        }

        void IosTextBox_EditingDidBegin(object _, EventArgs __)
        {
            Thread.Pool.Run(() => View.Focused.SetByInput(true));
        }

        void IosTextBox_EditingDidEnd(object _, EventArgs __)
        {
            DismissKeyboard(null, EventArgs.Empty);
            Thread.Pool.Run(() => View.Focused.SetByInput(false));
        }

        void IosTextBox_EditingChanged(object sender, EventArgs e)
        {
            var text = View.TextTransform.Apply(Text);
            Thread.Pool.RunAction(() => View.Value.SetByInput(text));
        }

        void Configure()
        {
            Text = View.TransformedText;
            Font = View.Font.Render();
            if (View.Placeholder.HasValue())
                AttributedPlaceholder = new NSAttributedString(View.Placeholder, null, (View.PlaceholderColor ?? Colors.LightGrey).Render());
            TextColor = View.TextColor.Render();
            BorderStyle = UITextBorderStyle.None;

            AutocapitalizationType = UITextAutocapitalizationType.None;
            UserInteractionEnabled = true;
            if (View.GetEffectiveTextMode() == TextMode.Password) SecureTextEntry = true;
            ReturnKeyType = View.KeyboardActionType.Render();
            SetAlignment();
            ShouldReturn += (v) => OnSubmitted();

            KeyboardType = View.GetEffectiveTextMode().RenderType();

            SetAutoCapitalization();
            SetSpellChecking();
            SetAutoCorrection();
        }

        void SetSpellChecking() => SpellCheckingType = View.SpellChecking.RenderSpellChecking();

        void SetAutoCapitalization() => AutocapitalizationType = View.AutoCapitalization.RenderAutocapitalization();

        void SetAutoCorrection() => AutocorrectionType = View.AutoCorrection.RenderAutoCorrection();

        bool OnSubmitted()
        {
            DismissKeyboard(null, EventArgs.Empty);

            if (View?.KeyboardActionType == KeyboardActionType.Next)
                View?.FocusOnNextInput();

            View?.UserTextChangeSubmitted.SignalRaiseOn(Thread.Pool);
            return true;
        }

        void SetSize()
        {
            var eff = View.Effective;

            EdgeInsets = new UIEdgeInsets(
              eff.BorderAndPaddingTop() - View.Font.GetUnwantedExtraTopPadding() / 2,
              eff.BorderAndPaddingLeft(),
               eff.BorderAndPaddingBottom(),
                eff.BorderAndPaddingRight());
        }

        public override CGRect TextRect(CGRect forBounds) => base.TextRect(InsetRect(forBounds, EdgeInsets));

        public override CGRect EditingRect(CGRect forBounds) => base.EditingRect(InsetRect(forBounds, EdgeInsets));

        public static CGRect InsetRect(CGRect rect, UIEdgeInsets insets)
        {
            return new CGRect(rect.X + insets.Left,
                                   rect.Y + insets.Top,
                                   rect.Width - insets.Left - insets.Right,
                                   rect.Height - insets.Top - insets.Bottom);
        }

        void SetAlignment()
        {
            VerticalAlignment = View.TextAlignment.RenderVerticalAlignment();
            TextAlignment = View.TextAlignment.Render();
        }

        void HandleEvents()
        {
            View.Tapped.HandleOnUI(Focus);
            View.Swiped.HandleOnUI(Focus);

            View.Value.HandleChangedBySourceOnUI(() => Text = View.TransformedText);

            View.TextAlignmentChanged.HandleOnUI(SetAlignment);
            View.Focused.HandleChangedBySourceOnUI(focused => { if (focused) BecomeFirstResponder(); else if (IsFirstResponder) ResignFirstResponder(); });

            View.PaddingChanged.HandleOnUI(SetSize);
            View.FontChanged.HandleOnUI(() => Font = View.Font.Render());
            View.PlaceholderColorChanged.HandleOnUI(() =>
           {
               if (View.Placeholder.HasValue())
                   AttributedPlaceholder = new NSAttributedString(View.Placeholder, null, View.PlaceholderColor.Render());
           });
            View.SpellCheckingChanged.HandleOnUI(SetSpellChecking);
            View.AutocapitalizationChanged.HandleOnUI(SetAutoCapitalization);
            View.AutoCorrectionChanged.HandleOnUI(SetAutoCorrection);
        }

        public void Apply(string property, UIChangedEventArgs change)
        {
            if (property == "TextColor")
                TextColor = (change as TextColorChangedEventArgs).Value.Render(); // TODO: Support animation

            else if (property == "Placeholder")
                Placeholder = (change as UIChangedEventArgs<string>).Value;
        }

        void Focus()
        {
            View.Focused.SetByInput(true);
            BecomeFirstResponder();
        }

        void DismissKeyboard(object _, EventArgs __)
        {
            if (IsFirstResponder) ResignFirstResponder();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                View = null;
                EditingDidBegin -= IosTextBox_EditingDidBegin;
                EditingDidEnd -= IosTextBox_EditingDidEnd;
                EditingChanged -= IosTextBox_EditingChanged;
                EditingDidEndOnExit -= DismissKeyboard;
            }

            base.Dispose(disposing);
        }
    }
}
namespace Zebble.IOS
{
    using System;
    using CoreGraphics;
    using Foundation;
    using UIKit;

    public class IosTextAreaWrapper : UIView, UIChangeCommand.IHandler
    {
        TextInput View;
        IosTextArea InnerView;

        public IosTextAreaWrapper(TextInput view) : base(view.GetFrame())
        {
            View = view;

            InnerView = new(View);

            AddSubview(InnerView);
        }

        public void Apply(string property, UIChangedEventArgs change)
        {
            if (property == "TextColor")
                InnerView.TextColor = (change as TextColorChangedEventArgs).Value.Render(); // TODO: Support animation
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                InnerView?.Dispose();
                InnerView = null;

                View = null;
            }

            base.Dispose(disposing);
        }

        class IosTextArea : UITextView
        {
            TextInput View;
            UILabel PlaceHolder;

            public IosTextArea(TextInput view) : base(view.GetFrame())
            {
                View = view;

                TextContainerInset = UIEdgeInsets.Zero;

                CreateInnerTextBox();
                HandleEvents();

                Frame = new CGRect(
                    view.Padding.Left.CurrentValue,
                    view.Padding.Top.CurrentValue,
                    view.ActualWidth - view.Padding.Horizontal(),
                    view.ActualHeight - view.Padding.Vertical());
            }

            void CreateInnerTextBox()
            {
                Text = View.TransformedText;
                TextColor = View.TextColor.Render();
                BackgroundColor = Colors.Transparent.Render();
                Font = View.Font.Render();
                AutocapitalizationType = UITextAutocapitalizationType.None;
                UserInteractionEnabled = true;
                Editable = true;
                ReturnKeyType = View.KeyboardActionType.Render();
                if (View.GetEffectiveTextMode() == TextMode.Password) SecureTextEntry = true;
                SetAlignment();

                Started += IosTextArea_Started;
                Ended += IosTextArea_Ended;
                Changed += IosTextArea_Changed;
                ShouldChangeText += OnShouldChangeText;

                CreatePlaceHolder();
            }

            async void IosTextArea_Changed(object sender, EventArgs e)
            {
                PlaceHolder.Hidden = Text?.Length > 0;

                var text = View.TextTransform.Apply(Text);
                await Thread.Pool.Run(() => View.Value.SetByInput(text));
            }

            bool OnShouldChangeText(UITextView _, NSRange __, string text)
            {
                if (text != "\n") return true;
                if (View.MultiLineAutoResize && View.KeyboardActionType == KeyboardActionType.Default) View.UserTappedOnReturnKey.Raise();
                var shouldComplete = View.UserTextChangeSubmitted.IsHandled() || View.KeyboardActionType == KeyboardActionType.Next;

                if (!shouldComplete) return true;

                CloseKeyboard();
                View.UserTextChangeSubmitted.SignalRaiseOn(Thread.Pool);
                if (View.KeyboardActionType == KeyboardActionType.Next)
                    View.FocusOnNextInput();

                return false;
            }

            void SetAlignment() => TextAlignment = View.TextAlignment.Render();

            void IosTextArea_Started(object _, EventArgs __)
            {
                BecomeFirstResponder();
                View.Focused.SetByInput(true);

                PlaceHolder.Hidden = Text?.Length > 0;
                Thread.Pool.Run(() => View.Focused.SetByInput(true));
            }

            void IosTextArea_Ended(object _, EventArgs __)
            {
                PlaceHolder.Hidden = Text?.Length > 0;
                Thread.Pool.Run(() => View.Focused.SetByInput(false));
            }

            void HandleEvents()
            {
                View.Tapped.HandleOnUI(Focus);
                View.Swiped.HandleOnUI(Focus);
                HandleApiChange(View.Value, ValueChanged);
                HandleApiChange(View.Focused, () => { if (View.Focused.Value) BecomeFirstResponder(); else ResignFirstResponder(); });
                View.FontChanged.HandleOnUI(() => Font = View.Font.Render());
                View.TextAlignmentChanged.HandleOnUI(SetAlignment);
            }

            void HandleApiChange<T>(Olive.TwoWayBindable<T> bindable, Action action)
            {
                bindable.ChangedBySource += () => Thread.UI.RunAction(action);
            }

            void CreatePlaceHolder()
            {
                PlaceHolder = new UILabel
                {
                    Text = View.Placeholder,
                    TextColor = (View.PlaceholderColor ?? Colors.LightGrey).Render(),
                    Font = View.Font.Render(),
                    TextAlignment = View.TextAlignment.Render(),
                    Lines = View.Lines,
                    LineBreakMode = UILineBreakMode.WordWrap,
                    Frame = new CGRect(
                        View.Padding.Left.CurrentValue,
                        View.Padding.Top.CurrentValue,
                        Frame.Width - (View.Padding.Left.CurrentValue + View.Padding.Right.CurrentValue),
                        Frame.Height - (View.Padding.Top.CurrentValue + View.Padding.Bottom.CurrentValue)
                    ),
                    Hidden = Text?.Length > 0
                };

                PlaceHolder.SizeToFit();
                AddSubview(PlaceHolder);
            }

            void CloseKeyboard()
            {
                View.Focused.SetByInput(false);
                ResignFirstResponder();
            }

            void Focus()
            {
                View.Focused.SetByInput(true);
                BecomeFirstResponder();
            }

            void ValueChanged()
            {
                Text = View.TransformedText;
                PlaceHolder.Hidden = Text?.Length > 0;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    Started -= IosTextArea_Started;
                    Ended -= IosTextArea_Ended;
                    Changed -= IosTextArea_Changed;
                    ShouldChangeText -= OnShouldChangeText;
                    View = null;
                }

                base.Dispose(disposing);
            }
        }
    }
}
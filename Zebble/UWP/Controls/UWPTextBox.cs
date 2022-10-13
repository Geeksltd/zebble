namespace Zebble.UWP
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Windows.UI.Text;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
    using controls = Windows.UI.Xaml.Controls;
    using xaml = Windows.UI.Xaml;
    using Olive;

    public abstract class UWPTextBoxBase<TResult> : IRenderOrchestrator, UIChangeCommand.IHandler
        where TResult : Control, new()
    {
        protected bool IsApiChangingText;
        readonly Renderer Renderer;
        protected TextInput View;
        protected TResult Result;

        protected UWPTextBoxBase(Renderer renderer)
        {
            Renderer = renderer;
            View = renderer.View as TextInput;
            GenerateResult();
            HandleEvents();
        }

        public Task<FrameworkElement> Render() => Task.FromResult<FrameworkElement>(Result);

        protected virtual void GenerateResult()
        {
            Result = new TResult
            {
                BorderThickness = new Thickness(0),
                Padding = new Thickness(0),
                Background = Colors.Transparent.RenderBrush(),
                Foreground = View.TextColor.RenderBrush()
            };

            Result.Loaded += Result_Loaded;
            Result.FocusEngaged += Result_FocusEngaged;
        }

        void Result_FocusEngaged(Control sender, FocusEngagedEventArgs args)
        {
            RemoveHighlightStyles();
            Result.FocusEngaged -= Result_FocusEngaged;
        }

        void Result_Loaded(object _, RoutedEventArgs __)
        {
            OnLoaded();
            Result.Loaded -= Result_Loaded;
        }

        TextBlock PlaceholderPresenter
        {
            get
            {
                return Result.FindChildInTemplate<TextBlock>("PlaceholderTextContentPresenter");
            }
        }

        Grid LayoutGrid => Result.FindChildInTemplate<Grid>();

        protected void OnLoaded()
        {
            ApplyFontAndPadding();
            SetAlignment();

            Result.FindChildInTemplate<Button>("DeleteButton")?.RemoveFromSuperview();
            var brush = View.PlaceholderColor.RenderBrush();
            PlaceholderPresenter.Set(x => x.Foreground = brush);
        }

        bool RemoveHighlightStyles()
        {
            if (View.IsDisposing) return true;

            var grid = Result.FindChildInTemplate<Grid>();
            if (grid is null)
            {
                // If the view is not actually shown, grid will be null.
                return false;
            }

            foreach (var state in VisualStateManager.GetVisualStateGroups(grid).SelectMany(x => x.States))
                state.Storyboard = new xaml.Media.Animation.Storyboard();

            return true;
        }

        void OnKeyDown(object sender, KeyRoutedEventArgs arg)
        {
            if (arg.Key != Windows.System.VirtualKey.Enter) return;

            if (View.MultiLineAutoResize && View.KeyboardActionType == KeyboardActionType.Default)
                View.UserTappedOnReturnKey.SignalRaiseOn(Thread.Pool);
            if (View.KeyboardActionType == KeyboardActionType.Next)
                View.FocusOnNextInput();

            if (!View.UserTextChangeSubmitted.IsHandled()) return;

            LoseFocus();
            arg.Handled = true;
            View.UserTextChangeSubmitted.SignalRaiseOn(Thread.Pool);
        }

        protected virtual void HandleEvents()
        {
            Result.AddHandler(TextBox.KeyDownEvent, new KeyEventHandler(OnKeyDown), handledEventsToo: true);

            Result.GotFocus += (s, e) => View.Focused.SetByInput(true);
            Result.LostFocus += (s, e) => View.Focused.SetByInput(false);
            Result.KeyDown += (s, e) => View.UserKeyDown.SignalRaiseOn(Thread.Pool, (int)e.Key);
            Result.KeyUp += (s, e) => View.UserKeyUp.SignalRaiseOn(Thread.Pool, (int)e.Key);

            View.TextAlignmentChanged.HandleOnUI(SetAlignment);
            View.Focused.HandleChangedBySourceOnUI(focused => { if (focused) Result.Focus(FocusState.Programmatic); else LoseFocus(); });

            View.FontChanged.HandleOnUI(ApplyFontAndPadding);
            View.PaddingChanged.HandleOnUI(ApplyFontAndPadding);
        }

        public void Apply(string property, UIChangedEventArgs change)
        {
            switch (property)
            {
                case "TextColor":
                    Result.Foreground = (change as TextColorChangedEventArgs).Value.RenderBrush(); // Consider animating it also if needed
                    break;
                case "Bounds":
                    View.SetSize(Result, (BoundsChangedEventArgs)change);
                    ApplyFontAndPadding();
                    break;
                case "Placeholder":
                    (Result as TextBox).Set(x => x.PlaceholderText = ((UIChangedEventArgs<string>)change).Value);
                    (Result as PasswordBox).Set(x => x.PlaceholderText = ((UIChangedEventArgs<string>)change).Value);
                    break;
            }
        }

        protected void ApplyFontAndPadding()
        {
            Result.RenderFont(View.Font);
            Result.Padding = View.Padding.RenderThickness();

            if (LayoutGrid != null)
            {
                var pushUp = new Thickness(0, -View.Font.GetUnwantedExtraTopPadding(), 0, 0);

                foreach (var child in LayoutGrid.Children.OfType<FrameworkElement>())
                {
                    if (child is ScrollViewer)
                        child.FindChildInTemplate<Border>().Margin = pushUp;
                    else child.Margin = pushUp;
                }
            }
        }

        void SetAlignment()
        {
            var textAlign = View.TextAlignment.RenderTextAlignment();

            var tb = Result as TextBox;
            if (tb != null && textAlign != tb.TextAlignment)
                tb.TextAlignment = textAlign;

            LayoutGrid.Set(x => x.VerticalAlignment = View.TextAlignment.RenderVerticalAlignment());

            PlaceholderPresenter.Set(x => x.HorizontalAlignment = View.TextAlignment.RenderHorizontalAlignment());
        }

        void LoseFocus()
        {
            var isTabStop = Result.IsTabStop;
            Result.IsTabStop = false;
            Result.IsEnabled = false;
            Result.IsEnabled = true;
            Result.IsTabStop = isTabStop;
        }

        public virtual void Dispose() => Result = null;
    }

    public class UWPTextBox : UWPTextBoxBase<TextBox>
    {
        public UWPTextBox(Renderer renderer) : base(renderer) { }

        protected override void GenerateResult()
        {
            base.GenerateResult();

            Result.PlaceholderText = View.Placeholder.OrEmpty();
            Result.TextWrapping = View.Lines > 1 ? TextWrapping.Wrap : TextWrapping.NoWrap;
            Result.AcceptsReturn = View.Lines > 1 && !View.UserTextChangeSubmitted.IsHandled();
            Result.Text = View.TransformedText;
            Result.InputScope = View.KeyboardActionType.Render(View.GetEffectiveTextMode());

            var spellChecking = View.SpellChecking.Render();
            var autoCapitalization = View.AutoCapitalization.Render();
            if (spellChecking && autoCapitalization)
            {
                Result.IsSpellCheckEnabled = true;
                Result.IsTextPredictionEnabled = false;
            }
            else
            {
                Result.IsSpellCheckEnabled = spellChecking;
                Result.IsTextPredictionEnabled = true;
            }
        }

        protected override void HandleEvents()
        {
            base.HandleEvents();

            Result.TextChanged += Result_TextChanged;

            View.Value.HandleChangedBySourceOnUI(() =>
            {
                IsApiChangingText = true;

                Result.Text = View.TransformedText;
                Thread.UI.Post(async () =>
                {
                    await Task.Delay(10);
                    IsApiChangingText = false;
                });
            });
        }

        void Result_TextChanged(object _, TextChangedEventArgs __)
        {
            if (IsApiChangingText) return;

            var text = View.TextTransform.Apply(Result.Text);
            Thread.Pool.Run(() => View.Value.SetByInput(text));
        }

        public override void Dispose()
        {
            Result.Set(x => x.TextChanged -= Result_TextChanged);
            base.Dispose();
        }
    }
}

namespace Zebble
{
    partial class TextInput
    {
        public readonly AsyncEvent<int> UserKeyUp = new();
        public readonly AsyncEvent<int> UserKeyDown = new();
    }
}
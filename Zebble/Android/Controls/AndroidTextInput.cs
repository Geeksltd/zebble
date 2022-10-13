namespace Zebble.AndroidOS
{
    using Android.Graphics;
    using Android.Runtime;
    using Android.Util;
    using Android.Views;
    using Android.Views.InputMethods;
    using Android.Widget;
    using Java.Lang;
    using Olive;
    using System;
    using System.Threading.Tasks;
    using Zebble.Device;

    public class AndroidTextInput : EditText, IPaddableControl
    {
        static AndroidTextInput CurrentlyFocused;
        TextInput View;
        bool IsApiChangingText;
        readonly AutoScrollUtility autoScrollUtility = null;
        public AndroidTextInput(TextInput view) : base(Renderer.Context)
        {
            View = view;
            CreateTextInput(view);
            HandleEvents(view);
            autoScrollUtility = new AutoScrollUtility(this);
        }

        [Preserve]
        public AndroidTextInput(IntPtr ptr, JniHandleOwnership handle) : base(ptr, handle)
        {
            autoScrollUtility = new AutoScrollUtility(this);
        }

        void CreateTextInput(TextInput view)
        {
            Append(view.TransformedText);
            SetTextColor(view.TextColor.Render());
            Gravity = view.TextAlignment.Render();
            TextDirection = TextDirection.Ltr;
            SetAllCaps(allCaps: view.TextTransform == TextTransform.Uppercase);

            SetLines(view.Lines);
            SetSingleLine(view.Lines < 2 || view.UserTextChangeSubmitted.IsHandled());

            InputType = view.GetEffectiveTextMode().RenderInputType();

            if (view.Lines > 1)
            {
                SetHorizontallyScrolling(whether: false);

                if (view.UserTextChangeSubmitted.IsHandled() || view.KeyboardActionType != KeyboardActionType.Default)
                {
                    SetMaxLines(view.Lines);
                }
                else
                {
                    InputType |= Android.Text.InputTypes.TextFlagMultiLine;

                    if (view.UserTextChangeSubmitted.IsHandled())
                        InputType |= Android.Text.InputTypes.TextFlagImeMultiLine;
                }
            }

            if (view.GetEffectiveTextMode() == TextMode.Password)
                TransformationMethod = Android.Text.Method.PasswordTransformationMethod.Instance;

            if (view.AutoCapitalization != AutoCapitalizationType.None)
                InputType |= view.AutoCapitalization.Render();

            InputType |= view.SpellChecking.Render();

            ImeOptions = view.KeyboardActionType.Render();
            Hint = view.Placeholder;
            Ellipsize = Android.Text.TextUtils.TruncateAt.End;
            SetHintTextColor((view.PlaceholderColor ?? Colors.LightGrey).Render());
            SetFont();

            ClearFocus();
            SetTextIsSelectable(selectable: true);
        }

        void HandleEvents(TextInput view)
        {
            view.Value.HandleChangedBySourceOnUI(() =>
            {
                IsApiChangingText = true;
                if (Text.HasValue()) Text = string.Empty;
                Append(view.TransformedText);
                IsApiChangingText = false;
            });

            view.TextAlignmentChanged.HandleOnUI(() => Gravity = view.TextAlignment.Render());
            view.Focused.HandleChangedBySourceOnUI(DoSetFocus);
            view.FontChanged.HandleOnUI(SetFont);
            view.PlaceholderColorChanged.HandleOnUI(() => SetHintTextColor(view.PlaceholderColor.Render()));

            AfterTextChanged += AndroidTextInput_AfterTextChanged;
            SetOnKeyListener(new OnKeyListener(view));
            SetOnTouchListener(new OnTouchListener(view));
        }

        public override void OnEditorAction([GeneratedEnum] ImeAction actionCode)
        {
            base.OnEditorAction(actionCode);

            if (IsDead(out var view)) return;

            DoSetFocus(focused: false);

            if (actionCode == ImeAction.Next) view.FocusOnNextInput();
            else Keyboard.Hide();

            view.UserTextChangeSubmitted.SignalRaiseOn(Zebble.Thread.Pool);
        }

        bool IsDead(out TextInput view)
        {
            view = View;
            if (view?.IsDisposing == true) view = null;
            if (view is null && autoScrollUtility != null)
                autoScrollUtility.Dispose();
            return view is null;
        }

        void AndroidTextInput_AfterTextChanged(object sender, Android.Text.AfterTextChangedEventArgs args)
        {
            if (IsDead(out var view)) return;
            if (IsApiChangingText) return;

            var text = view.TextTransform.Apply(Text);
            Zebble.Thread.Pool.RunAction(() => view.Value.SetByInput(text));
        }

        protected override void OnTextChanged(ICharSequence text, int start, int lengthBefore, int lengthAfter)
        {
            if (IsDead(out var view)) return;

            // Fix for Multi-line textbox enter key

            if (view.Lines == 1) return;
            if (IsApiChangingText) return;
            if (!view.UserTextChangeSubmitted.IsHandled()) return;
            if (text == null || text.Length() == 0) return;
            if (lengthAfter - lengthBefore != 1) return;
            if (text.Lacks('\n')) return;

            // Remove the enter character:
            IsApiChangingText = true;
            var cleanText = Text.KeepReplacing("\n", "");
            Text = "";
            Append(cleanText);
            IsApiChangingText = false;

            DoSetFocus(focused: false);
            view.UserTextChangeSubmitted.SignalRaiseOn(Zebble.Thread.Pool);
        }

        protected override void OnFocusChanged(bool gainFocus, [GeneratedEnum] FocusSearchDirection direction, Rect previouslyFocusedRect)
        {
            if (gainFocus) CurrentlyFocused = this;
            else if (CurrentlyFocused == this) CurrentlyFocused = null;

            base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);

            if (IsDead(out var view)) return;

            SetCursorVisible(gainFocus);

            view.Focused.SetByInput(gainFocus);

            if (gainFocus)
            {
                Keyboard.Show(view);
                DoSetFocus(true);
            }
            else HideKeyboardIfMine();
        }

        public override bool DispatchTouchEvent(MotionEvent eventArgs)
        {
            if (!IsDead(out var view) && view.Lines > 1)
                Parent?.RequestDisallowInterceptTouchEvent(disallowIntercept: true);

            return base.DispatchTouchEvent(eventArgs);
        }

        public override bool OnKeyPreIme([GeneratedEnum] Keycode keyCode, KeyEvent ev)
        {
            if (keyCode == Keycode.Back && ev.Action == KeyEventActions.Up)
            {
                var focused = UIRuntime.CurrentActivity.CurrentFocus;
                if (focused != null) (focused as EditText).ClearFocus();
            }

            return base.OnKeyPreIme(keyCode, ev);
        }

        void DoSetFocus(bool focused)
        {
            if (focused)
            {
                RequestFocus();
            }
            else
            {
                ClearFocus();
                HideKeyboardIfMine();
            }
        }

        void HideKeyboardIfMine()
        {
            Zebble.Thread.UI.Post(async () =>
            {
                await Task.Delay(100);
                if (CurrentlyFocused is null) Keyboard.Hide();
            });
        }

        void SetFont()
        {
            if (IsDead(out var view)) return;

            SetTextSize(ComplexUnitType.Px, Scale.ToDevice(view.Font.EffectiveSize));
            Typeface = view.Font.Render();
        }

        protected override void Dispose(bool disposing)
        {
            if (CurrentlyFocused == this) CurrentlyFocused = null;
            AfterTextChanged -= AndroidTextInput_AfterTextChanged;
            View = null;
            base.Dispose(disposing);
        }

        class OnKeyListener : Java.Lang.Object, IOnKeyListener
        {
            readonly TextInput View;
            public OnKeyListener(TextInput view) => View = view;

            public bool OnKey(View view, Keycode keyCode, KeyEvent args)
            {
                if (keyCode == Keycode.Enter) View.UserTappedOnReturnKey.Raise();
                if (keyCode == Keycode.Enter && View.UserTextChangeSubmitted.IsHandled() && !View.MultiLineAutoResize)
                {
                    View.UserTextChangeSubmitted.SignalRaiseOn(Zebble.Thread.Pool);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Overrides the default Android focusing problem if the user has clicked on an overlay.
        /// </summary>
        class OnTouchListener : Java.Lang.Object, IOnTouchListener
        {
            readonly TextInput View;

            public OnTouchListener(TextInput view) => View = view;

            AbstractAsyncEvent[] AllGestures()
            {
                return new AbstractAsyncEvent[] { View.Touched, View.Tapped, View.Swiped, View.LongPressed, View.Panning, View.Pinching, View.UserRotating, View.PanFinished };
            }

            /// <summary>
            /// Should return True if the listener has consumed the event, false otherwise.
            /// </summary>
            public bool OnTouch(View view, MotionEvent motionEvent)
            {
                if (motionEvent.Action != MotionEventActions.Down) return false;

                AllGestures().Do(x => x.HandleWith(DummyHandler));

                var gestureView = view.FindContainerGestureView();
                var newMotionEvent = GetMotionEvent(motionEvent, gestureView, View);

                AllGestures().Do(x => x.RemoveActionHandler(DummyHandler));

                var detectedView = gestureView.DetectHandler(newMotionEvent, View);
                if (detectedView != View)
                {
                    gestureView.GetGestureLayout().OnTouchEvent(newMotionEvent);
                    return true;
                }

                return false;
            }

            [EscapeGCop("This is meant to be an empty handler")]
            void DummyHandler() { }

            MotionEvent GetMotionEvent(MotionEvent motionEvent, IGestureView gestureView, TextInput textInput)
            {
                var gestureViewLeft = Scale.ToDevice(gestureView.GetHostView().CalculateAbsoluteX());
                var gestureViewTop = Scale.ToDevice(gestureView.GetHostView().CalculateAbsoluteY());
                var textInputLeft = Scale.ToDevice(textInput.CalculateAbsoluteX());
                var textInputTop = Scale.ToDevice(textInput.CalculateAbsoluteY());

                motionEvent.SetLocation(motionEvent.GetX() + textInputLeft - gestureViewLeft, motionEvent.GetY() + textInputTop - gestureViewTop);

                return motionEvent;
            }
        }
    }
}
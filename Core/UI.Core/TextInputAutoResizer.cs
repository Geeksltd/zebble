namespace Zebble
{
    using System;
    using System.Threading.Tasks;
    using Olive;

    class TextInputAutoResizer
    {
        TimeSpan ANIMATION_DURATION = 300.Milliseconds();

        float? OrginalActuallHeigh;
        int TextLineLength, PreviousCharacterLength, CharacterLength;
        int? MaxTextLineLen;
        TextInput View;

        public int MaxHeight;

        public TextInputAutoResizer(TextInput view, int maxHeight)
        {
            View = view;
            MaxHeight = maxHeight;

            HandleEvents();
        }

        void HandleEvents()
        {
            View.Value.ChangedByInput += OnUserTextChanged;
            if (!View.MultiLineAutoResize) View.UserTextChangeSubmitted.Handle(() => OnControlHeightChange());
            else View.UserTappedOnReturnKey.Handle(() => OnUserTappedOnReturnKey());
        }

        async Task MakeInputTextBigger()
        {
            var aditionalHeight = OrginalActuallHeigh - (View.Border.TotalVertical + View.Padding.Vertical());
            if (View.ActualHeight + aditionalHeight > MaxHeight) return;
            await View.Animate(ANIMATION_DURATION, b => b.Height(b.ActualHeight + aditionalHeight));
        }

        async Task MakeInputTextSmaller()
        {
            var aditionalHeight = OrginalActuallHeigh - (View.Border.TotalVertical + View.Padding.Vertical());
            if (View.ActualHeight - aditionalHeight < OrginalActuallHeigh) return;
            await View.Animate(ANIMATION_DURATION, b => b.Height(b.ActualHeight - aditionalHeight));
        }

        async void OnUserTextChanged()
        {
            if (View.Text.Length > MaxTextLineLen) return;
            if (View.Text.Length == 0 && OrginalActuallHeigh.HasValue) await OnControlHeightChange();

            var textWidth = View.Font.GetTextWidth(View.TransformedText);
            var bodyWidth = View.ActualWidth - (View.Border.TotalHorizontal + View.Padding.Horizontal());
            if (textWidth > bodyWidth && OrginalActuallHeigh is null)
            {
                TextLineLength = View.Text.Length;
                OrginalActuallHeigh = View.ActualHeight;
                CharacterLength = 0;
                await MakeInputTextBigger();
                return;
            }

            if (View.ActualHeight == View.AutoResizeMaxHeight && MaxTextLineLen is null)
                MaxTextLineLen = View.Text.Length;

            if (CharacterLength == TextLineLength && OrginalActuallHeigh.HasValue)
            {
                if (PreviousCharacterLength < View.Text.Length)
                {
                    CharacterLength = 0;
                    PreviousCharacterLength = View.Text.Length;
                    await MakeInputTextBigger();
                }
                else
                {
                    CharacterLength = 0;
                    PreviousCharacterLength = View.Text.Length;
                    await MakeInputTextSmaller();
                }
            }

            CharacterLength++;
        }

        async Task OnControlHeightChange() => await View.Animate(ANIMATION_DURATION, b => b.Height(OrginalActuallHeigh));

        async Task OnUserTappedOnReturnKey()
        {
            if (OrginalActuallHeigh is null)
            {
                var charWidth = View.Font.GetTextWidth("T");
                var bodyWidth = View.ActualWidth - (View.Border.TotalHorizontal + View.Padding.Horizontal());
                var charsPerLine = Math.Floor(bodyWidth / charWidth);
                var maxLines = Math.Floor(View.ActualHeight / View.Font.GetLineHeight());

                TextLineLength = (int)(charsPerLine * maxLines);
                OrginalActuallHeigh = View.ActualHeight;
            }

            CharacterLength = 0;
            PreviousCharacterLength = View.Text?.Length ?? 0;
            await MakeInputTextBigger();
        }
    }
}
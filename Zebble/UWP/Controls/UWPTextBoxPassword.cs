namespace Zebble.UWP
{
    using System;
    using controls = Windows.UI.Xaml.Controls;
    using Olive;

    public class UWPPasswordBox : UWPTextBoxBase<controls.PasswordBox>
    {
        public UWPPasswordBox(Renderer renderer) : base(renderer) { }

        protected override void GenerateResult()
        {
            base.GenerateResult();

            Result.PlaceholderText = View.Placeholder.OrEmpty();
            Result.Password = View.TransformedText;
        }

        protected override void HandleEvents()
        {
            base.HandleEvents();

            Result.PasswordChanged += (s, e) =>
            {
                if (IsApiChangingText) return;

                var text = View.TextTransform.Apply(Result.Password);
                Thread.Pool.RunAction(() => View.Value.SetByInput(text));
            };

            View.Value.HandleChangedBySourceOnUI(() => Result.Password = View.TransformedText);
        }
    }
}
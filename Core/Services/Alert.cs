namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Olive;

    public class Alert
    {
        static Prompt CurrentPrompt;
        public static string InputData => CurrentPrompt?.Input.Text;

        public static Task Show(string title, string message) => Show(title, message, awaitPopupClosing: false);

        public static Task Show(string title, string message, bool awaitPopupClosing)
            => Show(title, message, awaitPopupClosing, Pair.Of("OK", value: true));

        public static Task Show(string message) => Show(message, awaitPopupClosing: false);

        public static Task Show(string message, bool awaitPopupClosing) => Show(string.Empty, message, awaitPopupClosing);

        public static Task<bool> Confirm(string message)
            => Confirm(message, awaitPopupClosing: false);

        public static Task<bool> Confirm(string message, bool awaitPopupClosing)
            => Confirm("Please confirm", message, awaitPopupClosing);

        public static Task<bool> Confirm(string title, string message)
            => Confirm(title, message, awaitPopupClosing: false);

        public static Task<bool> Confirm(string title, string message, bool awaitPopupClosing)
        {
            return Show(title, message, awaitPopupClosing,
                 Pair.Of("Cancel", value: false),
                 Pair.Of("Confirm", value: true));
        }

        public static Task Toast(string message, bool showButton = true, Action<Toast> customise = null)
        {
            var toast = new Toast(message) { ShowButton = showButton };
            customise?.Invoke(toast);
            return toast.Show();
        }

        public static Task<T> Show<T>(string title, string message, params KeyValuePair<string, T>[] buttons)
            => Show(title, message, awaitPopupClosing: false, buttons: buttons);

        public static Task<T> Show<T>(string title, string message, bool awaitPopupClosing, params KeyValuePair<string, T>[] buttons)
        {
            return Show(title, message, awaitPopupClosing, null, buttons);
        }

        public static async Task<T> Show<T>(string title, string message, bool awaitPopupClosing, Action<AlertDialog> configure, params KeyValuePair<string, T>[] buttons)
        {
            var source = new TaskCompletionSource<T>();
            var alert = new AlertDialog(title, message);

            configure?.Invoke(alert);

            foreach (var b in buttons)
            {
                var button = await alert.ButtonsRow.Add(new Button
                {
                    Text = b.Key,
                    CssClass = "primary-button".OnlyWhen(b.Value?.ToStringOrEmpty() == "true")
                });

                button.On(x => x.Tapped, async () =>
                {
                    var closePopup = Nav.HidePopUp();
                    if (awaitPopupClosing) await closePopup;

                    source.TrySetResult(result: b.Value);
                });
            }

            Nav.ShowPopUp(alert).GetAwaiter();

            return await source.Task;
        }

        public static async Task<string> Prompt(string title, string description = null, Func<Prompt, Task> customise = null)
        {
            var source = new TaskCompletionSource<string>();

            var prompt = new Prompt(title, description);
            CurrentPrompt = prompt;

            prompt.OKButton.On(x => x.Tapped, async () =>
            {
                await Nav.HidePopUp();
                source.TrySetResult(prompt.Input.Text);
            });

            await prompt.WhenShown(() => prompt.Input.Focus());

            await (customise?.Invoke(prompt)).OrCompleted();

            await Nav.ShowPopUp(prompt);
            return await source.Task;
        }

        /// <param name="buttons">Key is the button text and value is the button action.</param> 
        public static Task<string> Prompt(string title, string description, KeyValuePair<string, Action>[] buttons)
        {
            async Task customise(Prompt prompt)
            {
                await prompt.OKButton.IgnoredAsync();
                await prompt.CancelButton.IgnoredAsync();

                foreach (var b in buttons)
                {
                    var defaultCssClass = "primary-button".OnlyWhen(b.Value?.ToStringOrEmpty() == "true");

                    var button = await prompt.ButtonsRow.Add(new Button
                    {
                        Text = b.Key,
                        CssClass = defaultCssClass
                    }.On(x => x.Tapped, b.Value));
                }
            }

            return Prompt(title, description, customise);
        }
    }
}
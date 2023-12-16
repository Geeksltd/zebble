namespace Zebble
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Olive;

    public interface IDialogs
    {
        Task ShowWaiting(bool block = true);
        Task HideWaiting(Guid? version = null);

        Task Toast(string message);

        Task<bool> Confirm(string message);
        Task<bool> Confirm(string title, string message);

        Task Alert(string message);
        Task Alert(string title, string message);

        Task<T> Decide<T>(string title, string message, params KeyValuePair<string, T>[] buttons);

        Task<string> Prompt(string title, string description);
    }

    public abstract class BaseDialogs : IDialogs
    {
        public Task ShowWaiting(bool block = true) => DoShowWaiting(block);

        protected abstract Task DoShowWaiting(bool block);

        public Task HideWaiting(Guid? version = null) => DoHideWaiting(version);

        protected abstract Task DoHideWaiting(Guid? version = null);

        public Task Toast(string message) => DoToast(message);

        protected abstract Task DoToast(string message);

        public Task<bool> Confirm(string message) => Confirm("Please confirm", message);

        public Task<bool> Confirm(string title, string message)
        {
            return Decide(title, message,
                 Pair.Of("Cancel", value: false),
                 Pair.Of("Confirm", value: true));
        }

        public Task Alert(string message) => Alert(null, message);

        public Task Alert(string title, string message)
            => Decide(title, message, Pair.Of("OK", value: true));

        public Task<T> Decide<T>(string title, string message, params KeyValuePair<string, T>[] buttons)
            => DoDecide(title, message, buttons);

        protected abstract Task<T> DoDecide<T>(string title, string message, params KeyValuePair<string, T>[] buttons);

        public Task<string> Prompt(string title, string description)
            => DoPrompt(title, description);

        protected abstract Task<string> DoPrompt(string title, string description);
    }

    class DefaultDialogs : BaseDialogs
    {
        protected override Task DoShowWaiting(bool block = true) => Waiting.Show(block);

        protected override Task DoHideWaiting(Guid? version = null) => Waiting.Hide(version);

        protected override Task DoToast(string message) => new ToastDialog(message).Show();

        protected override async Task<T> DoDecide<T>(string title, string message, params KeyValuePair<string, T>[] buttons)
        {
            var source = new TaskCompletionSource<T>();
            var alert = new AlertDialog(title, message);

            foreach (var b in buttons)
            {
                var button = await alert.ButtonsRow.Add(new Button
                {
                    Text = b.Key,
                    CssClass = "primary-button".OnlyWhen(b.Value?.ToStringOrEmpty() == "true"),
                    Id = b.Value.ToString().ToPascalCaseId()
                });

                button.On(x => x.Tapped, async () =>
                {
                    await Nav.HidePopUp();
                    source.TrySetResult(result: b.Value);
                });
            }

            await Nav.ShowPopUp(alert);

            return await source.Task;
        }

        protected override async Task<string> DoPrompt(string title, string description)
        {
            var source = new TaskCompletionSource<string>();

            var prompt = new PromptDialog(title, description);

            prompt.OKButton.On(x => x.Tapped, async () =>
            {
                await Nav.HidePopUp();
                source.TrySetResult(prompt.Input.Text);
            });

            await prompt.WhenShown(prompt.Input.Focus);

            await Nav.ShowPopUp(prompt);

            return await source.Task;
        }
    }

    public static class Dialogs
    {
        public static IDialogs Current = new DefaultDialogs();
    }
}
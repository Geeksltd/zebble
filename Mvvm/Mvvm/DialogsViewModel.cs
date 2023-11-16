using System.Collections.Generic;
using System.ComponentModel;

namespace Zebble.Mvvm
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public partial class DialogsViewModel
    {
        public static DialogsViewModel Current = new();

        public void ShowWaiting(bool block = true) => DoShowWaiting(block);

        public void HideWaiting() => DoHideWaiting();

        public void Toast(string message) => DoToast(message);

        public bool Confirm(string message) => Confirm("Please confirm", message);

        public bool Confirm(string title, string message)
            => Decide(title, message, Pair.Of("Cancel", false), Pair.Of("Confirm", true));

        public void Alert(string message) => Alert(null, message);

        public void Alert(string title, string message)
            => Decide(title, message, Pair.Of("OK", true));

        public T Decide<T>(string title, string message, params KeyValuePair<string, T>[] buttons)
            => DoDecide(title, message, buttons);

        public string Prompt(string title, string description = null)
            => DoPrompt(title, description);
    }

    partial class ViewModel
    {
        protected static DialogsViewModel Dialogs => DialogsViewModel.Current;
    }
}
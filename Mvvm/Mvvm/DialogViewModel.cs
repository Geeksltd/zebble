using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Zebble.Mvvm
{
    public interface IDialogViewModel
    {
        void ShowWaiting(bool block = true);
        void HideWaiting();
        void Toast(string message, bool showButton = true);
        void Alert(string title, string message);
        void Alert(string message);
        T Decide<T>(string title, string message, params KeyValuePair<string, T>[] buttons);
        bool Confirm(string message);
        bool Confirm(string title, string message);
        string Prompt(string title, string description = null);
        void ExpectAndTap(string text, string buttonToTap);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public partial class DialogViewModel : IDialogViewModel
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly DialogViewModel Current = new();

        public void ShowWaiting(bool block = true) => DoShowWaiting(block);

        public void HideWaiting() => DoHideWaiting();
        partial void DoHideWaiting();

        public string Prompt(string title, string description = null) => DoPrompt(title, description);

        public void Toast(string message, bool showButton = true) => DoToast(message, showButton);

        public void Alert(string message) => Alert("", message);

        public void Alert(string title, string message) => Decide(title, message, Pair.Of("OK", true));

        public bool Confirm(string message) => Confirm("Please confirm", message);

        public bool Confirm(string title, string message) => Decide(title, message, Pair.Of("Cancel", false), Pair.Of("Confirm", true));

        public T Decide<T>(string title, string message, params KeyValuePair<string, T>[] buttons)
        {
            var args = buttons.Select(x => Pair.Of<string, object>(x.Key, x.Value)).ToArray();
            return (T)DoDecide(title, message, typeof(T), args);
        }

        void IDialogViewModel.ExpectAndTap(string text, string buttonToTap)
            => throw new InvalidOperationException("ExpectAndTap is for automated testing and can be executed in VM runtime only.");
    }

    partial class ViewModel
    {
        protected IDialogViewModel Dialog { get; } = new DialogViewModel();
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Olive;

namespace Zebble.Mvvm
{
    partial class DialogsViewModel
    {
        void DoShowWaiting(bool block)
            => Dialogs.Current.ShowWaiting(block).RunInParallel();

        void DoHideWaiting()
            => Dialogs.Current.HideWaiting().RunInParallel();

        void DoToast(string message)
            => Dialogs.Current.Toast(message).RunInParallel();

        T DoDecide<T>(string title, string message, KeyValuePair<string, T>[] buttons)
            => Task.Factory.RunSync(() => Dialogs.Current.Decide(title, message, buttons));

        string DoPrompt(string title, string description)
            => Task.Factory.RunSync(() => Dialogs.Current.Prompt(title, description));

        public void ExpectAndTap(string text, string buttonToTap)
            => throw new InvalidOperationException("ExpectAndTap is for automated testing and can be executed in VM runtime only.");
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Olive;

namespace Zebble.Mvvm
{
    partial class DialogViewModel
    {
        string DoPrompt(string title, string description) => Task.Factory.RunSync(() => Zebble.Alert.Prompt(title, description));

        partial void DoHideWaiting() => Waiting.Hide().RunInParallel();

        void DoShowWaiting(bool block) => Waiting.Show(block).RunInParallel();

        void DoToast(string message, bool showButton) => Zebble.Alert.Toast(message, showButton).RunInParallel();

        void DoAlert(string title, string message) => Zebble.Alert.Show(title, message);

        object DoDecide(string title, string message, Type _, KeyValuePair<string, object>[] buttons)
        {
            return Task.Factory.RunSync(() => Zebble.Alert.Show(title, message, buttons));
        }
    }
}
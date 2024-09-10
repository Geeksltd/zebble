namespace Zebble.WinUI
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Services;
    using Olive;

    static class LiveCssWatcher
    {
        static bool IsDown;
        const string CSS_SERVER = "http://localhost:19765/Zebble/Css";

        internal static Task Start()
        {
            return Task.Factory.StartNew(async () =>
            {
                SendCommand("start");
                await StartWatching();
            }, TaskCreationOptions.LongRunning);
        }

        static async Task StartWatching()
        {
            await Task.Delay(4.Seconds());

            while (true)
            {
                await Task.Delay(1.Seconds());
                var changes = await DownloadStyleChanges();
                if (IsDown) return;

                await ApplyStyleChanges(changes);
            }
        }

        [DebuggerStepThrough]
        static async Task<string> DownloadStyleChanges()
        {
            try
            {
                var bytes = await Device.Network.Download(CSS_SERVER.AsUri(), retries: 1, timeoutPerAttempt: 3);
                if (bytes.Any()) return bytes.ToString(System.Text.Encoding.UTF8);
                return null;
            }
            catch (Exception ex)
            {
                IsDown = true;
                Log.For(typeof(LiveCssWatcher)).Error(ex, "Failed to download updated stylesheets.");
                Log.For(typeof(LiveCssWatcher)).Warning("Run the following in the command prompt, and start the app again → zebble-css watch");
                return null;
            }
        }

        static async Task ApplyStyleChanges(string changes)
        {
            if (changes.IsEmpty()) return;

            try
            {
                var all = changes.To<XElement>().Elements().ToArray();

                if (all.None()) return;

                CssEngine.ClearNonDynamics();

                foreach (var change in all)
                {
                    var rule = new RuntimeCssRule
                    {
                        File = change.GetValue<string>("@file"),
                        Selector = change.GetValue<string>("@selector"),
                        Platform = change.GetValue<string>("@platform").TryParseAs<DevicePlatform>(),
                        Body = change.GetValue<string>("@body")
                    };

                    CssEngine.Add(rule);
                }

                CssEngine.InspectionRules.Do(CssEngine.Add);

                await Nav.FullRefresh();
            }
            catch (Exception ex)
            {
                Log.For(typeof(LiveCssWatcher)).Error(ex, "Failed to reload stylesheets.");
            }
        }

        internal static void Exit() => SendCommand("exit");

        static void SendCommand(string command)
        {
            try
            {
                (WebRequest.Create(CSS_SERVER + "?" + command) as HttpWebRequest).Set(x => x.ContinueTimeout = 100)
                   .GetResponseAsync();
            }
            catch
            {
                // No logging is needed.
            }
        }
    }
}
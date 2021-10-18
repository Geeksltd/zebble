namespace Zebble.Css
{
    using Olive;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Xml.Linq;
    using Zebble.Tooling;

    class CssWatchService
    {
        static bool IsStarted, IsChanged;
        static object SyncLock = new object();
        List<FileSystemWatcher> Watchers = new List<FileSystemWatcher>();
        WebServer Server;

        internal void Start()
        {
            if (!Initialize()) return;
            if (!StartHttpServer()) return;

            WatchScssFiles();
            Console.ReadKey();
        }

        bool Initialize()
        {
            if (!DirectoryContext.AppUIFolder.Exists())
            {
                ConsoleHelpers.Error("UI folder not found at: " + DirectoryContext.AppUIFolder.FullName);
                return false;
            }

            return true;
        }

        void WatchScssFiles()
        {
            void detected(string f)
            {
                Console.WriteLine("Change detected: " + f);
                IsChanged = true;
            }

            CssManager.FindScssFiles(watch: true)
                .Select(x => x.Directory.FullName + Path.DirectorySeparatorChar)
                .Distinct()
                .Do(x =>
                   {
                       var watcher = new FileSystemWatcher(x, "*.scss");
                       watcher.Changed += (s, e) => detected(e.FullPath);
                       watcher.Renamed += (s, e) => detected(e.FullPath);
                       watcher.EnableRaisingEvents = true;
                       Watchers.Add(watcher);
                   });
        }

        bool StartHttpServer()
        {
            try
            {
                Server = new WebServer(SendResponse, "http://" + "localhost:19765/Zebble/Css/");
                Server.Run();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to register the web server: " + ex.Message);
                return false;
            }
        }

        void OpenFileInVs(string file)
        {
            var process = new Process();

            var startInfo = new ProcessStartInfo("devenv.exe", @$"/edit {DirectoryContext.AppUIStylesFolder.FullName}\{file.Split(":")[0]}")
            {
                WindowStyle = ProcessWindowStyle.Hidden
            };

            process.StartInfo = startInfo;
            process.Start();
        }

        public string SendResponse(HttpListenerRequest request)
        {
            if (request.RawUrl.EndsWith("?exit")) Environment.Exit(0);

            if (request.RawUrl.EndsWith("?start"))
            {
                IsStarted = true;
                IsChanged = false;
                return string.Empty;
            }

            if (request.RawUrl.IndexOf("?open=") > 0)
            {
                OpenFileInVs(request.RawUrl.Substring(request.RawUrl.IndexOf("?open=Style") + 13));
                return string.Empty;
            }

            if (!IsStarted || !IsChanged) return string.Empty;

            IsChanged = false;

            return LoadStyles().ToString();
        }

        XElement LoadStyles()
        {
            var result = new XElement("root");

            foreach (var rule in new CssManager().ExtractRules())
            {
                try
                {
                    result.Add(new XElement("rule",
                        new XAttribute("platform", rule.Platform.OrEmpty()),
                        new XAttribute("file", rule.File),
                        new XAttribute("selector", rule.Selector),
                        new XAttribute("body", rule.Body)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error extracting a rule: " + rule + " " + ex.Message);
                }
            }

            return result;
        }
    }
}
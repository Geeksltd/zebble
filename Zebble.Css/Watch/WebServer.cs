namespace Zebble.Css
{
    using Olive;
    using System;
    using System.Net;
    using System.Text;
    using System.Threading;
    using Zebble.Tooling;

    public class WebServer
    {
        readonly HttpListener Listener = new();
        readonly Func<HttpListenerRequest, string> ResponderMethod;

        public WebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
        {
            foreach (string s in prefixes) Listener.Prefixes.Add(s);

            ResponderMethod = method;
            Listener.Start();
        }

        public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
            : this(prefixes, method) { }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Console.WriteLine("Zebble started watching SCSS file changes for the following:");
                Console.WriteLine("--------------------------");

                foreach (var f in CssManager.FindScssFiles(watch: true))
                    Console.WriteLine(f.FullName.TrimStart(DirectoryContext.AppUIFolder.FullName));

                Console.WriteLine("--------------------------");
                Console.WriteLine("Awaiting changes...");

                try
                {
                    while (Listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;

                            try
                            {
                                var rstr = ResponderMethod(ctx.Request);
                                var buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch { } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, Listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }

        public void Stop()
        {
            Listener.Stop();
            Listener.Close();
        }
    }
}
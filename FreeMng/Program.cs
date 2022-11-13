using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeMng
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            CancellationToken ct = new CancellationToken();
            Console.WriteLine("Port 54400 is now listening... Press Ctrl+C to stop...");
            await Listen("http://+:54400/", 7000, ct);
        }
        public static async Task Listen(string prefix, int maxConcurrentRequests, CancellationToken token)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            var requests = new HashSet<Task>();
            for (int i = 0; i < maxConcurrentRequests; i++)
                requests.Add(listener.GetContextAsync());

            while (!token.IsCancellationRequested)
            {
                Task t = await Task.WhenAny(requests);
                requests.Remove(t);

                if (t is Task<HttpListenerContext>)
                {
                    var context = (t as Task<HttpListenerContext>).Result;
                    requests.Add(ProcessRequestAsync(context));
                    requests.Add(listener.GetContextAsync());
                }
            }
        }

        public static async Task ProcessRequestAsync(HttpListenerContext context)
        {
            await Task.Run(() =>
            {
                bool faviconset = false;
                bool shutdown = false;
                Console.WriteLine("Connection requested.");
                string r = null;
                if (context.Request.RawUrl == "/")
                {
                    r = @"
<html>
<head>
<title>FreeMng: Management Home</title>
</head>
<body>
<a href=""/reboot"">Reboot</a>
</body>
</html>
";
                }
                else if (context.Request.RawUrl == "/reboot")
                {
                    r = @"
<html>
<head>
<title>Rebooting</title>
</head>
<body>
Please wait while your server is rebooting...
</body>
</html>
";
                    shutdown = true;
                }
                else if (context.Request.RawUrl == "/about")
                {
                    r = @"
<html>
<head>
<title>About</title>
</head>
<body>
<h1>The FreeMng Project</h1>
<p>FreeMng 1.0.0.0</p>
<p>This is open-source project app. Anyone can do its copy and publish.</p>
</body>
</html>
";
                }
                else if (context.Request.RawUrl == "/favicon.ico")
                {
                    faviconset = true;
                }
                else
                {
                    r = @"<html>
<head>
<title>Not found</title>
</head>
<body>
Not found!
</body>
</html>";
                    context.Response.StatusCode = 404;
                }
                byte[] gg = null;
                if (faviconset == false)
                {
                    context.Response.ContentType = "text/html";
                    gg = Encoding.UTF8.GetBytes(r);
                }
                else
                {
                    gg = Files.FaviconBytes();
                    context.Response.ContentType = "image/x-icon";
                }
                context.Response.ContentLength64 = gg.LongLength;
                context.Response.OutputStream.Write(gg, 0, gg.Length);
                context.Response.OutputStream.Close();
                if (shutdown == true)
                {
                    Process.Start("shutdown", "/r /f /t 0");
                }
            });
        }
    }
}

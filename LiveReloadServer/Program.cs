using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Westwind.Utilities;

namespace LiveReloadServer
{
    public class Program
    {

        public static IHost WebHost;

        public static string Version { get; set;  }

        public static void Main(string[] args)
        {
            if (Environment.CommandLine.Contains("LiveReloadWebServer", StringComparison.InvariantCultureIgnoreCase))
                Helpers.ExeName = "LiveReloadWebServer";

            var cmdLine = Environment.CommandLine;

            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                var ver = version.Major + "." + version.Minor +
                          (version.Build > 0 ? "." + version.Build : string.Empty);
                Helpers.AppHeader = $"Live Reload Web Server v{ver}";

                Version = ver;

                // Process commands that don't start the server
                if (ProcessNonServerCommandLineSwitches(cmdLine))
                {
                    return;
                }

                var builder = CreateHostBuilder(args);
                if (builder == null)
                    return;

                WebHost = builder.Build();
                
                WebHost.Run();
            }
            catch (IOException ex)
            {
                
                ColorConsole.WriteWarning("\r\nUnable to start the Web Server.");
                Console.WriteLine("------------------------------");
                ColorConsole.WriteWrappedHeader("\r\nUnable to start the Web Server.",headerColor: ConsoleColor.DarkYellow);

                Console.WriteLine("Most likely the server port is already in use by another application.");
                Console.WriteLine("Please try and choose another port with the `--port` switch. And try again.");
                Console.WriteLine("\r\n\r\n");
                ColorConsole.WriteError(ex.Message);
                Console.WriteLine("---------------------------------------------------------------------------");
            }
            catch (SocketException ex)
            {
                ColorConsole.WriteError("\r\nUnable to start the Web Server.");
                Console.WriteLine("------------------------------");

                Console.WriteLine("The server Host IP address is invalid.");
                Console.WriteLine("Please try and choose another host IP address with the `--host` switch. And try again.");
                Console.WriteLine("\r\n\r\n");
                ColorConsole.WriteError(ex.Message);
                Console.WriteLine("---------------------------------------------------------------------------");
            }
            catch (Exception ex)
            {
                // can't catch internal type
                if (ex.StackTrace.Contains("ThrowOperationCanceledException"))
                    return;

                WriteStartupErrorMessage(ex.Message, ex.StackTrace, ex.Source);
            }
        }

        /// <summary>
        /// Pre-process Command Line Switches that don't start up the server.
        /// 
        /// Returns true to indicate processing is complete, false to continue
        /// on into the server.
        /// </summary>
        /// <param name="cmdLine"></param>
        /// <returns>True - processing complete, False - continue running</returns>
        private static bool ProcessNonServerCommandLineSwitches(string cmdLine)
        {
            if (cmdLine.Contains("--help", StringComparison.OrdinalIgnoreCase) ||
                cmdLine.Contains(" /h") || cmdLine.Contains(" -h"))
            {
                ShowHelp();
                return true;
            }
            if (cmdLine.Contains("-RegisterExplorer", StringComparison.OrdinalIgnoreCase))
            {
                Startup.RegisterInExplorer();
                return true;
            }
            if (cmdLine.Contains("-UnregisterExplorer", StringComparison.OrdinalIgnoreCase))
            {
                Startup.RegisterInExplorer(true);
                return true;
            }
            if (cmdLine.Contains("-OpenSettings", StringComparison.OrdinalIgnoreCase))
            {
                var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "LiveReloadWebServer.json");
                ShellUtils.ShellExecute(path);
                return true;
            }

            return false;
        }


        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {

                    // Custom Config
                    var config = new ConfigurationBuilder()
                        .AddJsonFile("LiveReloadServer.json", optional: true)
                        .AddJsonFile("LiveReloadWebServer.json", optional: true)
                        .AddEnvironmentVariables()
                        .AddEnvironmentVariables("LIVERELOADSERVER_")
                        .AddEnvironmentVariables("LIVERELOADWEBSERVER_")
                        .AddCommandLine(args)
                        .Build();

                    var serverConfig = new LiveReloadServerConfiguration();
                    serverConfig.LoadFromConfiguration(config);

                    if (!serverConfig.ShowConsoleOutput)
                    {
                        Console.SetError(TextWriter.Null);
                        Console.SetOut(TextWriter.Null);
                    }


                    var webRoot = serverConfig.WebRoot;
                    if (!Directory.Exists(webRoot))
                    {
                        throw new ApplicationException($"Launch Error: WebRoot '{webRoot}' is not a valid directory.");
                    }

                    // Custom Logging
                    webBuilder
                        .ConfigureLogging(logging =>
                        {
                            logging.ClearProviders();
                            if (serverConfig.ShowConsoleOutput)
                                logging.AddConsole();
                            logging.AddConfiguration(config);
                        })
                        .UseConfiguration(config);

                    if (!string.IsNullOrEmpty(webRoot))
                        webBuilder.UseWebRoot(webRoot);
                  
                    var virtualPath = serverConfig.VirtualPath;
                    if (!string.IsNullOrEmpty(virtualPath) || virtualPath == "/")
                        virtualPath = "";

                    var hostingUrl = serverConfig.GetHostingHostingServerUrl();
                    webBuilder.UseUrls(hostingUrl);
                        //$"http{(serverConfig.UseSsl ? "s" : "")}://{serverConfig.Host}:{serverConfig.Port}{virtualPath}");
                    
                    webBuilder
                        .UseStartup<Startup>();
                });
        }


        
        public static void WriteStartupErrorMessage(string message, string stackTrace = null, string source = null)
        {
            string headerLine = new string('-', Helpers.AppHeader.Length);
            Console.WriteLine(headerLine);
            Console.WriteLine(Helpers.AppHeader);
            Console.WriteLine(headerLine);

            ColorConsole.WriteWarning("\r\nSomething went wrong during server startup!");
            Console.WriteLine();

            Console.WriteLine("The Live Reload Server has run into a problem and has stopped working.\r\n");
            ColorConsole.WriteError(message);

            if (!string.IsNullOrEmpty(stackTrace))
            {
                Console.WriteLine("----- Error Info -----");
                Console.WriteLine(stackTrace);
                Console.WriteLine(source);
                Console.WriteLine("----------------------");
            }
        }

        static void ShowHelp()
        {

            string razorFlag = null;
            bool useRazor = false;
#if USE_RAZORPAGES
            razorFlag = "\r\n--UseRazor           True|False*";
            useRazor = true;
#endif

            string headerLine = new string('-', Helpers.AppHeader.Length);

            Console.WriteLine($@"
{headerLine}
{Helpers.AppHeader}
{headerLine}");

            ColorConsole.WriteLine("(c) Rick Strahl, West Wind Technologies, 2019-" + DateTime.Now.Year, ConsoleColor.DarkGray);

            Console.WriteLine($@"Static, Markdown and Razor Files Web Server with Live Reload for changed content.

Syntax:
-------
{Helpers.ExeName} <WebRoot> <options>

--WebRoot                <path>  (current Path if not provided)
--Port                   0*|5210  0* - use next available port >= 5200
                                  n  - force a specific port number)
--Host                   0.0.0.0*|localhost|custom Ip - 0.0.0.0 allows external access
--UseSsl                 True|False*{razorFlag}

--UseLiveReload          True*|False
--Extensions             ""{(useRazor ? ".cshtml," : "")}.css,.js,.htm,.html,.ts""*
--DefaultFiles           ""index.html,default.htm""*
--FolderNotFoundFallbackPath 
                         ""Fallback Url on 404 folder requests (none* or ""index.html"")""

--ShowUrls               True|False*
--OpenBrowser            True*|False
--BrowserUrl             optional startup URL (relative or absolute)
--OpenEditor             True|False*
--EditorLaunchCommand    ""code \""%1\""""* (Win)
                         ""open - a \""Visual Studio Code\"" \""%1\""""* (Mac)
--DetailedErrors         True*|False
--ShowConsoleOutput      True*|False (turn off for production)
--Environment            Production*|Development
--VirtualPath            / | /docs/ | /myApp/docs/  (default is root /)

Razor Pages:
------------
--UseRazor              True|False*

Markdown Options:
-----------------
--UseMarkdown           True|False*  
--CopyMarkdownResources True|False*  
--MarkdownTemplate      ~/markdown-themes/__MarkdownTestmplatePage.cshtml*
--MarkdownTheme         github*|dharkan|medium|blackout|westwind
--MarkdownSyntaxTheme   github*|vs2015|vs|monokai|monokai-sublime|twilight

System
------
-RegisterExplorer       Registers Open LiveReload Web Server Here Folder Shell Context Menu
-UnRegisterExplorer     Unregisters Shell integration
-OpenSettings           Opens default Configuration Settings in an editor as Json

Options can be specified in this order in:

* Configuration File
* Environment Variables with '{Helpers.ExeName.ToUpper()}_' prefix. Example: '{Helpers.ExeName.ToUpper()}_PORT'
* Command Line options as shown above
* Logical Command Line Flags for true can be set like: -UseSsl or -UseRazor or -OpenBrowser


Examples:
---------
{Helpers.ExeName} ""c:\temp\My Site"" -useSsl -useRazor 
{Helpers.ExeName} --WebRoot ""c:\temp\My Html Site"" --port 5500 -useSsl -openEditor

$env:{Helpers.ExeName}_Port 5500
$env:{Helpers.ExeName}_WebRoot c:\mySites\Site1\Web
{Helpers.ExeName}
");
        }


#region External Access

        public static void Start(string[] args)
        {
            var builder = CreateHostBuilder(args);
            if (builder == null)
                return;

            WebHost = builder.Build();
            WebHost.Start();
        }

        public static void Stop()
        {
            WebHost.StopAsync().GetAwaiter().GetResult();
        }

#endregion
        
    }


}

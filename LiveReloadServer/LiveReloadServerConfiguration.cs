using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Westwind.Utilities;
using System.Net.NetworkInformation;

namespace LiveReloadServer
{
    public class LiveReloadServerConfiguration //: LiveReloadConfiguration
    {

        public static LiveReloadServerConfiguration Current;

        /// <summary>
        /// The WebRoot folder that is used to serve Web content from.
        /// If not specified the launching folder is used.
        /// </summary>
        public string WebRoot { get; set; }

        
        /// <summary>
        /// An optional virtual path that can be applied when the server starts
        /// so you can emulate a Web site subfolder (ie. /docs/)
        /// 
        /// Automatically adds a trailing slash to the path
        /// </summary>
        public string VirtualPath {
            get => _virtualPath;
            set {
                
                if (string.IsNullOrEmpty(value))
                    value = "/";

                _virtualPath = StringUtils.TerminateString(value,"/");                
            }
        }
        private string _virtualPath = "/";

        /// <summary>
        /// When set registers LiveReloadServer as an Explorer Folder Open here
        /// </summary>
        public bool RegisterExplorer { get; set; }

        /// <summary>
        /// If set unregisters the Explorer shell extension to open a Web site here
        /// </summary>
        public bool UnregisterExplorer { get; set; }


        /// <summary>
        /// The port that the server is bound to.
        /// 
        /// Defaults to 0 which attempts to find an empty port 
        /// starting at port 5200.
        /// </summary>
        public int Port { get; set; } = 0;

        /// <summary>
        /// The IP or Host address to bind the server to. By default uses localhost
        /// which is **local only**. To bind to a publicly exposed IP address
        /// use an explicit IP address or `0.0.0.0`
        /// </summary>
        public string Host { get; set; } = "localhost";


        /// <summary>
        /// Determines whether the server is launched with SSL support
        /// </summary>
        public bool UseSsl { get; set; } = false;


        /// <summary>
        /// Determines whether pages are refreshed when a change is made to any of the
        /// monitored file extensions set in `ClientFileExtensions`.
        /// </summary>
        public bool UseLiveReload { get; set; } = true;

        /// <summary>
        /// Extensions monitored for live reload
        /// </summary>
        public string Extensions { get; set; } = ".cshtml,.css,.js,.htm,.html,.ts,.razor";

        /// <summary>
        /// Determines whether Razor pages are executed. Razor pages must
        /// be self contained without 'code behind models'.
        /// </summary>
        public bool UseRazor { get; set; } = false;


        /// <summary>
        /// Default page files to redirect to in extensionless URLs if the files exist in folder
        /// </summary>
        public string DefaultFiles { get; set; } = "index.html,index.htm,default.htm";

        /// <summary>
        /// An optional fallback path that's used in the LiveReloadServer
        /// to redirect to another URL if a folder isn't found on the server.
        /// Used for SPA application that have client navigation URLS and
        /// need to redirect back a starting page.
        /// Example: "/index.html"
        /// </summary>
        public string FolderNotFoundFallbackPath { get; set; }


        /// <summary>
        /// Determines whether the browser is opened
        /// </summary>
        public bool OpenBrowser { get; set; } = true;


        /// <summary>
        /// Optional browser URL that can be used to open an explict page in the browser.
        /// Can be actual URL or relative (ie `/testpage.html`)
        /// </summary>
        public string BrowserUrl {get; set; } = null;


        /// <summary>
        /// If true starts up the configured editor. Default editor is Vs Code
        /// </summary>
        public bool OpenEditor {get; set; } = false;

        /// <summary>
        /// If specified - opens the LiveReloadServer.json settings file
        /// </summary>
        public bool OpenSettings { get; set; }


        /// <summary>
        /// Launch Command used to launch an editor when using -OpenEditor switch
        /// </summary>
        public string EditorLaunchCommand { get; set; }


        /// <summary>
        /// Finds a suitable External Windows editor that supports folders
        /// for editing. Can be customized by setting the `EditorLaunchCommand`.
        /// 
        /// Note that Code and Cursor are launched using their launcher apps.
        /// Unfortunately VS Code ends up often leaving behind a stray terminal
        /// window that doesn't close.         
        /// </summary>
        /// <returns></returns>
        public static string FindWindowsExternalEditor()
        {

            var path64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var editor = Path.Combine(path64, "Microsoft VS Code", "code.exe");
            if (File.Exists(editor))
                return editor; // "Code"; // return the code launcher not the EXE   // editor;

            var localAppPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            editor = Path.Combine(localAppPath, "Microsoft VS Code", "code.exe");
            if (File.Exists(editor))
                return editor; // "Code"; // return Code launcher not the exe // editor   -

            editor = Path.Combine(path64, "Notepad++", "Notepad++.exe");
            if (File.Exists(editor))
                return editor;

            editor = Path.Combine(localAppPath, "Programs", "Cursor", "Cursor.exe");
            if (File.Exists(editor))
                return editor; //  "Cursor"; //  return Cursor launcher not the exe  // editor;

            return "Notepad.exe";
        }


        /// <summary>
        /// Determines whether the server console window shows the URLs
        /// </summary>
        public bool ShowUrls {get; set; } = true;


        /// <summary>
        /// Console output should be turned off for production
        /// </summary>
        public bool ShowConsoleOutput { get; set; } = true;

        /// <summary>
        /// Determines if detailed error information is shown in
        /// the console display.
        /// </summary>
        public bool DetailedErrors {get; set; } = true;

        #region Markdown Settings

        /// <summary>
        /// Determines whether Markdown processing is enabled
        /// </summary>
        public bool UseMarkdown { get; set; } = false;

        /// <summary>
        /// When set copies the Markdown Resources into the
        /// </summary>
        public bool CopyMarkdownResources { get; set; } = false;

        public string MarkdownTemplate { get; set; } = "~/markdown-themes/__MarkdownPageTemplate.cshtml";

        /// <summary>
        /// Page theme used by the provide Markdown page wrapper themes:
        /// github*|dharkan|medium|blackout|westwind
        ///
        /// Themes can be customized by creating a new folder in the
        /// </summary>
        public string MarkdownTheme { get; set; } = "github";

        /// <summary>
        /// Theme used for source code syntax coloring.
        /// github*|dharkan|medium|blackout|westwind
        /// </summary>
        public string MarkdownSyntaxTheme { get; set; } = "github";

        #endregion

        #region Server Settings

        /// <summary>
        /// Allows you to set additional Mime mappings for use in the LiveReload Server
        /// Console application. Format:
        /// ".dll"    - "application/octet-stream"
        /// ".custom" - "text/plain"
        /// </summary>
        public Dictionary<string,string> AdditionalMimeMappings {get; set; }


        #endregion

        /// <summary>
        /// Error message set when load fails
        /// </summary>
        public string ErrorMessage = null;

        /// <summary>
        /// Loads and fixes up configuration values from the configuraton.
        /// </summary>
        /// <remarks>
        /// Note we're custom loading this to allow for not overly string command line syntax
        /// </remarks>
        /// <param name="Configuration">.NET Core Configuration Provider</param>
        public bool LoadFromConfiguration(IConfiguration Configuration)
        {
            Current = this;

            WebRoot = Configuration["WebRoot"];
            if (string.IsNullOrEmpty(WebRoot))
            {
                // if not set but the first arg does not start with the - it's the folder
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 1 && !args[1].StartsWith("-"))
                    WebRoot = args[1];
            }
            if (string.IsNullOrEmpty(WebRoot))
                WebRoot = Environment.CurrentDirectory;
            else
            {
                var expandedPath = FileUtils.ExpandPathEnvironmentVariables(WebRoot);
                WebRoot = Path.GetFullPath(expandedPath, Environment.CurrentDirectory);
            }
            
            Port = Helpers.GetIntegerSetting("Port", Configuration, Port);
            if (Port == 0)
            {
                // pick a random port that's available                
                Port = FindLowestAvailablePort();
            }

            VirtualPath = Helpers.GetStringSetting("VirtualPath", Configuration, VirtualPath);
            UseSsl = Helpers.GetLogicalSetting("UseSsl", Configuration, UseSsl);
            Host = Helpers.GetStringSetting("Host", Configuration, Host);
            DefaultFiles = Helpers.GetStringSetting("DefaultFiles", Configuration, DefaultFiles);
            Extensions = Helpers.GetStringSetting("Extensions", Configuration, Extensions);
            
            UseLiveReload = Helpers.GetLogicalSetting("UseLiveReload", Configuration, UseLiveReload);
            UseRazor = Helpers.GetLogicalSetting("UseRazor", Configuration);
            ShowUrls = Helpers.GetLogicalSetting("ShowUrls", Configuration, ShowUrls);
            ShowConsoleOutput = Helpers.GetLogicalSetting("ShowConsoleOutput", Configuration, ShowConsoleOutput);

            
            OpenBrowser = Helpers.GetLogicalSetting("OpenBrowser", Configuration, OpenBrowser);
            BrowserUrl = Helpers.GetStringSetting("BrowserUrl", Configuration, BrowserUrl);
            OpenEditor =Helpers.GetLogicalSetting("OpenEditor", Configuration, OpenEditor);
            EditorLaunchCommand = Helpers.GetStringSetting("EditorLaunchCommand", Configuration);
            if (string.IsNullOrEmpty(EditorLaunchCommand))
            {
                EditorLaunchCommand =
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
                        "open -a \"Visual Studio Code\" \"%1\"" :
                        "\"" + FindWindowsExternalEditor() + "\" \"%1\"";
            }

            
            RegisterExplorer = Helpers.GetLogicalSetting("RegisterExplorer", Configuration, RegisterExplorer);
            UnregisterExplorer = Helpers.GetLogicalSetting("UnregisterExplorer", Configuration, UnregisterExplorer);
            OpenSettings = Helpers.GetLogicalSetting("OpenSettings", Configuration, OpenSettings);


            DetailedErrors = Helpers.GetLogicalSetting("DetailedErrors", Configuration, DetailedErrors);

            FolderNotFoundFallbackPath = Helpers.GetStringSetting("FolderNotFoundFallbackPath",Configuration,null);

            // Enables Markdown Middleware and optionally copies Markdown Templates into output folder
            UseMarkdown = Helpers.GetLogicalSetting("UseMarkdown", Configuration, false);
            if (UseMarkdown)
            {
                // defaults to true but only if Markdown is enabled!
                CopyMarkdownResources = Helpers.GetLogicalSetting("CopyMarkdownResources", Configuration, CopyMarkdownResources);
            }
            MarkdownTemplate = Helpers.GetStringSetting("MarkdownTemplate", Configuration, MarkdownTemplate);
            MarkdownTheme = Helpers.GetStringSetting("MarkdownTheme", Configuration, MarkdownTheme);
            MarkdownSyntaxTheme = Helpers.GetStringSetting("MarkdownSyntaxTheme", Configuration, MarkdownSyntaxTheme);


            // Fix ups
            if (Extensions is null)
                Extensions = string.Empty;

            if (UseMarkdown)
            {
                if (!Extensions.Contains(".md"))
                    Extensions += ",.md";
                if (!Extensions.Contains(".markdown"))
                    Extensions += ",.markdown";

                if (!DefaultFiles.Contains(".md"))
                    DefaultFiles = DefaultFiles.Trim(',') + ",README.md,index.md";
            }

           
            return true;
        }

        

        /// <summary>
        /// Returns the Host displayname for a browser URL. Fixes up local
        /// host names of `127.0.0.1` and `0.0.0.0` and `*` as `localhost`
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public string GetHostName(string host = null)
        {
            if(host == null)
                host = Host;

            if (string.IsNullOrEmpty(host) || host == "0.0.0.0" || host == "127.0.0.1" || host == "*")
                return "localhost";

            return host;
        }

        /// <summary>
        /// Retrieves the adjusted HTTP URL - adjusted for localhost for
        /// local urls that can be safely used in the browser.
        /// </summary>
        /// <returns></returns>
        public string GetHttpUrl(bool noHostNameTranslation = false)
        {
            var hostName = Host;
            if (!noHostNameTranslation)
                hostName = GetHostName();

            return $"http{(UseSsl ? "s" : "")}://{hostName}:{Port}{VirtualPath}";
        }

        /// <summary>
        /// Retrieve the hosting server URL used to start the server
        /// </summary>
        /// <returns></returns>
        public string GetHostingHostingServerUrl()
        {         
            return $"http{(UseSsl ? "s" : "")}://{Host}:{Port}";
        }


        private static int FindLowestAvailablePort(int startPort = 5200, int endPort = 10000)
        {
            for (int port = startPort; port <= endPort; port++)
            {
                if (IsPortAvailable(port))
                    return port;
            }

            return -1;
        }

        private static bool IsPortAvailable(int port)
        {
            try
            {
                using (TcpListener listener = new TcpListener(IPAddress.Loopback, port))
                {
                    listener.Start();
                    listener.Stop();
                    return true;
                }
            }
            catch (SocketException)
            {
                return false; // Port is in use
            }
        }

        private class ByteArrayComparer : IComparer<byte[]>
        {
            public int Compare(byte[] x, byte[] y)
            {
                if (x == null || y == null) return 0;
                for (int i = 0; i < Math.Min(x.Length, y.Length); i++)
                {
                    int cmp = x[i].CompareTo(y[i]);
                    if (cmp != 0) return cmp;
                }
                return x.Length.CompareTo(y.Length);
            }
        }


    }

}

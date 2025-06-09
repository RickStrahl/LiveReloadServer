using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Westwind.AspNetCore.LiveReload;
using Westwind.AspNetCore.Markdown;
using Westwind.Utilities;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.AspNetCore.Hosting;


namespace LiveReloadServer
{
    public class Startup
    {
        /// <summary>
        /// Binary Startup Location irrespective of the environment path
        /// </summary>
        public static string StartupPath { get; set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            StartupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public IConfiguration Configuration { get; }

        public LiveReloadServerConfiguration ServerConfig { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ServerConfig = new LiveReloadServerConfiguration();
            ServerConfig.LoadFromConfiguration(Configuration);           

            if (ServerConfig.UseLiveReload)
            {
                services.AddLiveReload(opt =>
                {
                    opt.FolderToMonitor = ServerConfig.WebRoot;
                    opt.LiveReloadEnabled = ServerConfig.UseLiveReload;

                    if (!string.IsNullOrEmpty(ServerConfig.Extensions))
                        opt.ClientFileExtensions = ServerConfig.Extensions;                
                });
            }

            IMvcBuilder mvcBuilder = null;

#if USE_RAZORPAGES
            if (ServerConfig.UseRazor)
            {
                mvcBuilder = services.AddRazorPages(opt => { opt.RootDirectory = "/"; })
                    .AddRazorRuntimeCompilation(
                        opt =>
                        {
                            opt.FileProviders.Clear();
                            opt.FileProviders.Add(new PhysicalFileProvider(ServerConfig.WebRoot));
                            opt.FileProviders.Add(
                                new PhysicalFileProvider(Path.Combine(Startup.StartupPath, "templates")));
                        });
            }
#endif

            if (ServerConfig.UseMarkdown)
            {
                services.AddMarkdown(config =>
                {
                    var templatePath = ServerConfig.MarkdownTemplate;
                    templatePath = templatePath.Replace("\\", "/");

                    var folderConfig = config.AddMarkdownProcessingFolder("/", templatePath);

                    // Optional configuration settings
                    folderConfig.ProcessExtensionlessUrls = true; // default
                    folderConfig.ProcessMdFiles = true; // default

                    folderConfig.RenderTheme = ServerConfig.MarkdownTheme;
                    folderConfig.SyntaxTheme = ServerConfig.MarkdownSyntaxTheme;
                });


                // we have to force MVC in order for the controller routing to work
                mvcBuilder = services
                    .AddMvc()
                    .AddRazorRuntimeCompilation(
                        opt =>
                        {
                            opt.FileProviders.Clear();
                            opt.FileProviders.Add(new PhysicalFileProvider(ServerConfig.WebRoot));
                            opt.FileProviders.Add(
                                new PhysicalFileProvider(Path.Combine(Startup.StartupPath, "templates")));
                        })
                    // have to let MVC know we have a dynamically loaded controller
                    .AddApplicationPart(typeof(MarkdownPageProcessorMiddleware).Assembly);

                // copy Markdown Template and resources if it doesn't exist
                if (ServerConfig.CopyMarkdownResources)
                    CopyMarkdownTemplateResources();
            }

            if (mvcBuilder != null)
            {
                //mvcBuilder.AddRazorRuntimeCompilation(
                //    opt =>
                //    {
                //        opt.FileProviders.Clear();
                //        opt.FileProviders.Add(new PhysicalFileProvider(ServerConfig.WebRoot));
                //        opt.FileProviders.Add(
                //            new PhysicalFileProvider(Path.Combine(Startup.StartupPath, "templates")));
                //    });

                // explicitly add any custom assemblies so Razor can see them for compilation                
                LoadNugetPackages(mvcBuilder, ServerConfig);
                LoadPrivateBinAssemblies(mvcBuilder);
            }
        }


        private static object consoleLock = new object();

        
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {

            if (ServerConfig.UseLiveReload)
                app.UseLiveReload();

            if (ServerConfig.DetailedErrors)
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Error");

            if (ServerConfig.ShowUrls)
                app.Use(DisplayRequestInfoMiddlewareHandler);

            if (ServerConfig.VirtualPath != "/") 
                app.UsePathBase(ServerConfig.VirtualPath);

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = new PhysicalFileProvider(ServerConfig.WebRoot),
                DefaultFileNames = new List<string>(ServerConfig.DefaultFiles.Split(',', ';'))
            });

            if (ServerConfig.UseMarkdown)
                app.UseMarkdown();

            // add static files to WebRoot and our templates folder which provides markdown templates
            // and potentially other library resources in the future

            var wrProvider = new PhysicalFileProvider(ServerConfig.WebRoot);
            var tpProvider = new PhysicalFileProvider(Path.Combine(Startup.StartupPath, "templates"));

            var extensionProvider = new FileExtensionContentTypeProvider();
            extensionProvider.Mappings.Add(".dll", "application/octet-stream");

            if (ServerConfig.AdditionalMimeMappings != null)
            {
                foreach (var map in ServerConfig.AdditionalMimeMappings)
                    extensionProvider.Mappings[map.Key] = map.Value;
            }

            var compositeProvider = new CompositeFileProvider(wrProvider, tpProvider);
            var staticFileOptions = new StaticFileOptions
            {
                //FileProvider = compositeProvider, //new PhysicalFileProvider(WebRoot),

                FileProvider = new PhysicalFileProvider(ServerConfig.WebRoot),
                RequestPath = new PathString(""),
                ContentTypeProvider = extensionProvider,
                DefaultContentType = "application/octet-stream"
            };
            app.UseStaticFiles(staticFileOptions);


            if (ServerConfig.UseRazor || ServerConfig.UseMarkdown || !string.IsNullOrEmpty(ServerConfig.FolderNotFoundFallbackPath))
                app.UseRouting();

#if USE_RAZORPAGES
            if (ServerConfig.UseRazor)
            {
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapRazorPages();
                });
            }
#endif
            if (ServerConfig.UseMarkdown)
            {
                app.UseEndpoints(endpoints =>
                {
                    // We need MVC Routing for Markdown to work
                    endpoints.MapDefaultControllerRoute();
                });
            }

           
            if(!string.IsNullOrEmpty(ServerConfig.FolderNotFoundFallbackPath))
            {
                app.UseEndpoints(endpoints =>
                {

                    if (!string.IsNullOrEmpty(ServerConfig.FolderNotFoundFallbackPath))
                    {
                        endpoints.MapFallbackToFile(ServerConfig.FolderNotFoundFallbackPath);
                        //app.Use(FallbackMiddlewareHandler);
                    }
                });
            }

            DisplayServerSettings(env);

            if (ServerConfig.OpenBrowser)
            {
                OpenBrowser();
            }

            if (ServerConfig.OpenEditor)
            {
                OpenEditor();
            }
        }


        /// <summary>
        /// Copies the Markdown Template resources into the WebRoot if it doesn't exist already.
        ///
        /// If you want to get a new set of template, delete the `markdown-themes` folder in hte
        /// WebRoot output folder.
        /// </summary>
        /// <returns>false if already exists and no files were copied. True if path doesn't exist and files were copied</returns>
        private bool CopyMarkdownTemplateResources()
        {
            // explicitly don't want to copy resources
            if (!ServerConfig.CopyMarkdownResources)
                return false;

            var templatePath = Path.Combine(ServerConfig.WebRoot, "markdown-themes");
            if (Directory.Exists(templatePath))
                return false;

            FileUtils.CopyDirectory(Path.Combine(Startup.StartupPath, "templates", "markdown-themes"),
                templatePath, recursive: true);

            return true;
        }


        #region Console Display Operations
        private void DisplayServerSettings(IWebHostEnvironment env)
        {
            string headerLine = new string('-', Helpers.AppHeader.Length);
            Console.WriteLine(headerLine);
            ColorConsole.WriteLine(Helpers.AppHeader, ConsoleColor.Yellow);
            Console.WriteLine(headerLine);
            Console.WriteLine($"(c) West Wind Technologies, 2019-{DateTime.Now.Year}\r\n");

            string displayUrl = ServerConfig.GetHttpUrl();
            string hostUrl = ServerConfig.GetHttpUrl(true);
            if (hostUrl == displayUrl || hostUrl.Contains("127.0.0.1"))
                hostUrl = null;
            else
            {
                hostUrl = $"  [darkgray](binding: {hostUrl})[/darkgray]";
            }

            ColorConsole.WriteEmbeddedColorLine($"Site Url     : [darkcyan]{ServerConfig.GetHttpUrl()}[/darkcyan] {hostUrl}");
            //ConsoleHelper.WriteLine(, ConsoleColor.DarkCyan);
            Console.WriteLine($"Web Root     : {ServerConfig.WebRoot}");            
            Console.WriteLine($"Executable   : {Assembly.GetExecutingAssembly().Location}");
            Console.WriteLine($"Live Reload  : {ServerConfig.UseLiveReload}");
            if (ServerConfig.UseLiveReload)
                Console.WriteLine(
                    $"  Extensions : {(string.IsNullOrEmpty(ServerConfig.Extensions) ? $"{(ServerConfig.UseRazor ? ".cshtml," : "")}.css,.js,.htm,.html,.ts" : ServerConfig.Extensions)}");


#if USE_RAZORPAGES
            Console.WriteLine($"Use Razor    : {ServerConfig.UseRazor}");
#endif

            Console.WriteLine($"Use Markdown : {ServerConfig.UseMarkdown}");
            if (ServerConfig.UseMarkdown)
            {
                Console.WriteLine($"  Resources  : {ServerConfig.CopyMarkdownResources}");
                Console.WriteLine($"  Template   : {ServerConfig.MarkdownTemplate}");
                Console.WriteLine($"  Theme      : {ServerConfig.MarkdownTheme}");
                Console.WriteLine($"  SyntaxTheme: {ServerConfig.MarkdownSyntaxTheme}");
            }

            Console.WriteLine($"Show Urls    : {ServerConfig.ShowUrls}");
            Console.Write($"Open Browser : {ServerConfig.OpenBrowser}");

            Console.WriteLine($"   Default Pages: {ServerConfig.DefaultFiles}");
            Console.WriteLine($"Detail Errors: {ServerConfig.DetailedErrors}");
            ColorConsole.WriteEmbeddedColorLine($"Environment  : [darkgreen]{env.EnvironmentName}[/darkgreen]  {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

            Console.WriteLine();
            ColorConsole.Write(Helpers.ExeName + " --help", ConsoleColor.DarkCyan);
            Console.WriteLine(" for start options...");
            Console.WriteLine();
            ColorConsole.WriteLine("Ctrl-C or Ctrl-Break to exit...", ConsoleColor.Yellow);

            Console.WriteLine("----------------------------------------------");

            var oldColor = Console.ForegroundColor;
            foreach (var assmbly in LoadedPrivateAssemblies)
            {
                var fname = Path.GetFileName(assmbly);
                ColorConsole.WriteEmbeddedColorLine("Additional Assembly: [darkgreen]" + fname + "[/darkgreen]");
            }
            foreach (var assmbly in FailedPrivateAssemblies)
            {
                var fname = Path.GetFileName(assmbly);
                ColorConsole.WriteEmbeddedColorLine("Failed Additional Assembly: [red]" + fname + "[/red]");
            }
            foreach (var package in LoadedNugetPackages)
            {
                var fname = Path.GetFileName(package);
                if (fname.StartsWith("--"))
                    ColorConsole.WriteEmbeddedColorLine("Loaded Nuget assembly: [darkgreen]" + fname + "[/darkgreen]");
                else 
                    ColorConsole.WriteEmbeddedColorLine("Loaded Nuget Package: [darkgreen]" + fname + "[/darkgreen]");
            }
            foreach (var package in FailedNugetPackages)
            {
                var fname = Path.GetFileName(package);
                if (fname.StartsWith("--"))
                    ColorConsole.WriteEmbeddedColorLine("Failed to load Nuget assembly: [red]" + fname + "[/red]");
                else
                    ColorConsole.WriteEmbeddedColorLine("Failed to load Nuget Package: [red]" + fname + "[/red]");
            }

            Console.ForegroundColor = oldColor;
        }

        /// <summary>
        /// This middle ware handler intercepts every request captures the time
        /// and then logs out to the screen (when that feature is enabled) the active
        /// request path, the status, processing time.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        async Task DisplayRequestInfoMiddlewareHandler(HttpContext context, Func<Task> next)
        {
            var originalPath = context.Request.Path.Value;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            await next();

            // ignore Web socket requests
            if (context.Request.Path.Value == LiveReloadConfiguration.Current?.WebSocketUrl)
                return;

            // need to ensure this happens all at once otherwise multiple threads
            // write intermixed console output on simultaneous requests
            lock (consoleLock)
            {
                WriteConsoleLogDisplay(context, sw, originalPath);
            }
        }

        /// <summary>
        /// Responsible for writing the actual request display out to the console.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sw"></param>
        /// <param name="originalPath"></param>
        private void WriteConsoleLogDisplay(HttpContext context, Stopwatch sw, string originalPath)
        {
            var url =
                $"{context.Request.Method}  {context.Request.Scheme}://{context.Request.Host}  {originalPath}{context.Request.QueryString}";

            url = url.PadRight(80, ' ');

            var ct = context.Response.ContentType;
            bool isPrimary = ct != null &&
                             (ct.StartsWith("text/html") ||
                              ct.StartsWith("text/plain") ||
                              ct.StartsWith("application/json") ||
                              ct.StartsWith("text/xml"));


            var saveColor = Console.ForegroundColor;

            var status = context.Response.StatusCode;

            if (ct == null) // no response
            {
                ColorConsole.Write(url + " ", ConsoleColor.Red);
                isPrimary = true;
            }
            else if (isPrimary)
                ColorConsole.Write(url + " ", ConsoleColor.Gray);
            else
                ColorConsole.Write(url + " ", ConsoleColor.DarkGray);


            if (status >= 200 && status < 400)
                ColorConsole.Write(status.ToString(),
                    isPrimary ? ConsoleColor.Green : ConsoleColor.DarkGreen);
            else if (status == 401)
                ColorConsole.Write(status.ToString(),
                    isPrimary ? ConsoleColor.Yellow : ConsoleColor.DarkYellow);
            else if (status >= 400)
                ColorConsole.Write(status.ToString(), isPrimary ? ConsoleColor.Red : ConsoleColor.DarkRed);

            sw.Stop();
            ColorConsole.WriteLine($" {sw.ElapsedMilliseconds:n0}ms".PadLeft(8), ConsoleColor.DarkGray);


            Console.ForegroundColor = saveColor;
        }

        #endregion


        #region NuGet And AssemblyLoading and PrivateBin Updates

        private void LoadNugetPackages(IMvcBuilder mvcBuilder, LiveReloadServerConfiguration config)
        {

            var binPath = Path.Combine(ServerConfig.WebRoot, "privatebin");
            var jsonFile = Path.Combine(binPath, "NugetPackages.json");
            var outputPath = Path.Combine(binPath, "Nuget");
            if (!System.IO.File.Exists(jsonFile))
                return;

            var packageConfiguration = JsonSerializationUtils.DeserializeFromFile<NuGetPackageConfiguration>(jsonFile);                
            if (packageConfiguration == null)
                return; 
            if (packageConfiguration.NugetSources == null || packageConfiguration.NugetSources.Count == 0)
                packageConfiguration.NugetSources = new() { "https://api.nuget.sorg/v3/index.json" };                                   
            

            var nuget = new NuGetPackageLoader(outputPath);

            Task.Run(async () =>
            {
                foreach (var package in packageConfiguration.Packages)
                {
                    try
                    {
                        await nuget.LoadPackageAsync(package.PackageId, 
                            package.Version, 
                            mvcBuilder, 
                            packageConfiguration.NugetSources, 
                            LoadedNugetPackages, FailedNugetPackages);
                    }
                    catch
                    {
                        FailedNugetPackages.Add(package.PackageId);
                    }
                }
            }).FireAndForget(); 

        }



        

        private List<string> LoadedPrivateAssemblies = new List<string>();
        private List<string> FailedPrivateAssemblies = new List<string>();
        private List<string> LoadedNugetPackages = new List<string>();
        private List<string> FailedNugetPackages = new List<string>();

        private void LoadPrivateBinAssemblies(IMvcBuilder mvcBuilder)
        {
            var binPath = Path.Combine(ServerConfig.WebRoot, "privatebin");
            var updatePath = Path.Combine(binPath, "updates");

            // if 
            UpdatePrivateBinAssemblies();

            
            if (Directory.Exists(binPath))
            {
                var files = Directory.GetFiles(binPath);
                foreach (var file in files)
                {
                    if (!file.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase) &&
                        !file.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    try
                    {
                        var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
                        mvcBuilder.AddApplicationPart(asm);
                        LoadedPrivateAssemblies.Add(file);
                    }
                    catch (Exception ex)
                    {
                        FailedPrivateAssemblies.Add(file + "\n    - " + ex.Message);
                    }

                }
            }
        
        }


        FileSystemWatcher privateBinWatcher { get; set; }

        private void UpdatePrivateBinAssemblies()
        {
            var updatePath = Path.Combine(ServerConfig.WebRoot, "privatebin", "updates");

            // If Updates Folder exists copy files into PrivateBin
            if (Directory.Exists(updatePath))
            {
                var files = Directory.GetFiles(updatePath);
                foreach (var file in files)
                {
                    try
                    {
                        var updateFile = file.Replace("\\updates\\", "\\").Replace("/updates/","/");
                        File.Copy(file, updateFile, true);
                        File.Delete(file);
                        
                        ColorConsole.WriteLine("Assembly Updated: " + file, ConsoleColor.Green);
                    }
                    catch(Exception ex) { 
                        ColorConsole.WriteLine("Assembly Update failed:  " + file + ". " + ex.Message, ConsoleColor.Red);
                    }
                }

                // On IIS allow updates in /privatebin/updates folder when updated by touching web.config
                if (privateBinWatcher == null) // && File.Exists(Path.Combine(ServerConfig.WebRoot,"web.config")))
                {
                    privateBinWatcher = new FileSystemWatcher(updatePath, filter: "*.dll");
                    privateBinWatcher.EnableRaisingEvents = true;                    

                    privateBinWatcher.IncludeSubdirectories = false;
                    privateBinWatcher.NotifyFilter = NotifyFilters.LastWrite;
                    privateBinWatcher.Changed += OnPrivateBinPrivateBinIisWatcherOnChanged;
                    privateBinWatcher.Created += OnPrivateBinPrivateBinIisWatcherOnChanged;                    
                }
            }
        }

        private void OnPrivateBinPrivateBinIisWatcherOnChanged(object sender, FileSystemEventArgs args)
        {
            var file = Path.Combine(ServerConfig.WebRoot, "web.config");   
            Console.WriteLine("Trying to update Assemblies by touching web.config: " + file);
            var fi = new FileInfo(file);
            if (!fi.Exists)
                return;

            Console.WriteLine("updating: " + file);

            // trigger IIS to recycle
            fi.LastWriteTime = DateTime.Now;                        
        }

        #endregion


        #region Shell Operations
        /// <summary>
        /// Opens the Web Browser on the local machine using Shell Operations
        /// </summary>
        private void OpenBrowser()
        {
            var rootUrl = ServerConfig.GetHttpUrl();

            if (!string.IsNullOrEmpty(ServerConfig.BrowserUrl))
            {
                if (ServerConfig.BrowserUrl.StartsWith("http://") || ServerConfig.BrowserUrl.StartsWith("https://"))
                    rootUrl = ServerConfig.BrowserUrl;
                else
                {
                    var url = "/" + ServerConfig.BrowserUrl.TrimStart('/'); // force leading slash
                    rootUrl = rootUrl.TrimEnd('/') + url
                        .Replace("~", "")
                        .Replace("\\", "/")
                        .Replace("//", "/");
                }
            }
            Helpers.OpenUrl(rootUrl);
        }


        /// <summary>
        /// Opens the configured editor (if any) on the local machine
        /// </summary>
        private void OpenEditor()
        {
            string cmdLine = null;
            try
            {                
                cmdLine = ServerConfig.EditorLaunchCommand.Replace("%1", ServerConfig.WebRoot);                               
                ShellUtils.ExecuteCommandLine(cmdLine);
            }
            catch (Exception ex)
            {
                ColorConsole.WriteError("Failed to launch editor with: " + cmdLine);
                ColorConsole.WriteError("-- " + ex.Message);
            }
        }


        public static void RegisterInExplorer(bool unregister = false)
        {

            // [HKEY_CURRENT_USER\Software\Classes\Directory\shell\LiveReloadServer]
            // @="Open as Web Site w/ LiveReloadServer"
            // "Icon"="livereloadWebServer.exe,0"
            // 
            // [HKEY_CURRENT_USER\Software\Classes\Directory\LiveReloadServer\command]
            // @="LiveReloadServer.exe \"%1\ --openBrowser""

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;


            if (!unregister)
            {
                try
                {
                    var location = Assembly.GetExecutingAssembly().Location;


                    string exec = location;
                    if (File.Exists(location.Replace(".dll", ".exe")))
                        exec = location.Replace(".dll", ".exe");
                    else
                    {
                        exec = Path.Combine(Environment.ExpandEnvironmentVariables("%USERPROFILE%"), ".dotnet", "tools", "LiveReloadServer.exe");                        
                        if (!File.Exists(exec))
                            exec = location;    
                    }                        

                    var key = @"HKEY_CURRENT_USER\Software\Classes\Directory\shell\LiveReloadServer";
                    var value = "Open as Live Reload Web Site";
                    Microsoft.Win32.Registry.SetValue(key, "", value);
                    
                    value = "\"" + Path.GetDirectoryName(location) + "\\LiveReload.ico\"";
                    Microsoft.Win32.Registry.SetValue( key, "Icon", value);

                    key = @"HKEY_CURRENT_USER\Software\Classes\Directory\shell\LiveReloadServer\command";
                    value = "\"" + exec + "\" \"%1\"";
                    Microsoft.Win32.Registry.SetValue( key, "", value);

                    ColorConsole.WriteSuccess("Registered LiveReloadServer from Windows Explorer.");
                }
                catch(Exception ex)
                {
                    ColorConsole.WriteError("Failed to register LiveReloadServer in Windows Explorer. " + ex.Message);
                }
            }
            else
            {
                try
                {         
                    var keyPath = @"Software\Classes\Directory\shell";
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath, writable: true))
                    {
                        if (key != null)
                        {
                            key.DeleteSubKeyTree("LiveReloadServer", throwOnMissingSubKey: false);
                        }
                        ColorConsole.WriteSuccess("Unregistered LiveReloadServer from Windows Explorer.");
                    }
                }
                catch
                {
                    ColorConsole.WriteError("Failed to unregister LiveReloadServer in Windows Explorer.");
                }
            }

        }


        #endregion


        #region Middleware

        /// <summary>
        /// Fallback handler middleware that is fired for any requests that aren't processed.
        /// This ends up being either a 404 result or
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        private async Task FallbackMiddlewareHandler(HttpContext context, Func<Task> next)
        {
            // 404 - no match
            if (string.IsNullOrEmpty(ServerConfig.FolderNotFoundFallbackPath))
            {
                await Status404Page(context);
                return;
            }

            // 404  - SPA fall through middleware - for SPA apps should fallback to index.html
            var path = context.Request.Path;
            if (string.IsNullOrEmpty(Path.GetExtension(path)))
            {
                var file = Path.Combine(ServerConfig.WebRoot,
                    ServerConfig.FolderNotFoundFallbackPath.Trim('/', '\\'));
                var fi = new FileInfo(file);
                if (fi.Exists)
                {
                    if (!context.Response.HasStarted)
                    {
                        context.Response.ContentType = "text/html";
                        context.Response.StatusCode = 200;
                    }

                    await context.Response.SendFileAsync(new PhysicalFileInfo(fi));
                    await context.Response.CompleteAsync();
                }
                else
                {
                    await Status404Page(context,isFallback: true);
                }
            }


        }

        /// <summary>
        /// Writes out a 404 error page with 404 error status
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task Status404Page(HttpContext context, string additionalHtml = null, bool isFallback = false)
        {
            if (string.IsNullOrEmpty(additionalHtml) && isFallback)
            {
                additionalHtml = @"
<p>A fallback URL is configured but was not found: <b>{ServerConfig.FolderNotFoundFallbackPath}</b></p>

<p>The fallback URL can be used for SPA application to handle server side redirection to the initial
client side startup URL - typically <code>/index.html</code>. This URL is loaded if
a URL cannot otherwise be handled. This error page is displayed because the specified fallback
page or resource also does not exist.</p>
";
            }

            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 404;
                context.Response.ContentType = "text/html";
            }

            await context.Response.WriteAsync(@$"
<html>
<body style='font-family: sans-serif; margin:2em 5%; max-width: 978px;'>
<h1>Not Found</h1>
<hr/>
<p>The page or resource you are accessing is not available on this site.<p>

{additionalHtml}

</body></html>");
            await context.Response.CompleteAsync();
        }
    }

    #endregion
}


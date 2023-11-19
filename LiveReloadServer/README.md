# Live Reload Web Server

**A self-contained, local, cross-platform, static file Web Server with automatic Live Reloading, Markdown rendering and loose Razor Pages support.**

This server supports:

* Generic Static File Web Server you can launch in any folder
* Just start with:
    * `LiveReloadServer <folder>` (dotnet tool)  
    * `LiveReloadWebServer <folder>` (installed version)
* LiveReload functionality for change detection and browser refresh
* Self-contained Razor Pages support with Live Reload Support
* Themed Markdown page rendering support built in
* Options to customize location, port, files checked etc.
* Easily installed and updated with `dotnet tool -g install LiveReloadServer`
* Run local SPA applications (Angular, VueJs, React etc.)
* Run Blazor Applications (without Live Reload support however)
* Cross Platform - Windows, Mac, Linux (dotnet tool only)
* Serve HTTPS content (dotnet tool only)
* Hostable ASP.NET Core app that can be used by multiple sites on a server
* Available as: Dotnet Tool, Chocolatey Package, or Self-Contained (Windows) Download

* [More detailed info on Github](https://github.com/RickStrahl/LiveReloadServer)

### Links
* [v1 Release Blog Post](https://weblog.west-wind.com/posts/2021/Mar/23/LiveReloadServer-A-NET-Core-Based-Generic-Static-Web-Server-with-Live-Reload#Feedback)

### Requirements:

* Dotnet Tool: .NET 7, 6 or 5 SDK
* Hosted: .NET 7
* Standalone Exe (Window): self-contained (.NET 7)
* If optionally hosting requires a Web Server that supports WebSockets

You can grab the compiled tool as:

* [Dotnet Tool](https://www.nuget.org/packages/LiveReloadServer/)  <small>(windows, mac, linux)</small>  
  ```ps
  dotnet tool install -g LiveReloadServer
  ```
* [Chocolatey Package](https://chocolatey.org/packages/LiveReloadWebServer) <small>(windows)</small>
  ```ps
  choco install LiveReloadWebServer
  ```
* [Self Contained Windows Executable Folder (zipped)](https://github.com/RickStrahl/LiveReloadServer/raw/master/LiveReloadWebServer-SelfContained.zip) <small>(windows)</small>
* [Hostable Package (requires installed .NET 7.0 Runtime)](https://github.com/RickStrahl/LiveReloadServer/raw/master/LiveReloadServer-Hosted.zip) <small>(windows, mac, linux)</small>  

> All three versions have the same features and interface, just the delivery mechanism and the executable name is different. The EXE uses `LiveReloadWebServer` while the Dotnet Tool uses `LiveReloadServer`.
  
### What does it do?
This tool is a generic **local Web Server** that you can point to **any folder** and provide simple and quick HTTP access to HTML and other Web resources. You can serve any **static resources** - HTML, CSS, JS etc. - as well as **loose Razor Pages** that don't require any code behind or dependent source code. There's also optional support for rendering **Markdown Pages** as themed HTML directly from Markdown files.

Live Reload is enabled by default and checks for changes to common static files. If a checked file is changed, the browser's current page is refreshed. You can map additional extensions that trigger the LiveReload.

You can also use this 'generic' server behind a live Web Server (like IIS, nginx etc.) by installing the main project as a deployed Web application to provide loose Razor support and Markdown rendering on a Web server. A  single LiveReloadServer installation can serve many Web sites using the same static, Razor and Markdown resources which can be ideal for mostly static content sites that need 'a little extra' beyond plain static pages (examples [here](https://anti-trust.rocks) and [here](https://markdownmonster.west-wind.com)).

## Installation
You can install this server as a .NET Tool using Dotnet SDK Tool installation:

```powershell
dotnet tool install -g LiveReloadServer
```

To use it, navigate to a folder that you want to serve HTTP files out of:

```ps
# will serve current folder files out of http://localhost:5200
LiveReloadServer

# specify a folder instead of current folder and a different port
LiveReloadServer "c:/temp/My Local WebSite" --port 5350 -UseSsl

# Customize some options
LiveReloadServer --LiveReloadEnabled False --OpenBrowser False -UseSsl -UseRazor
```

You can also install from Chocolatey:

```ps
choco install LiveReloadWebServer
```

Note that EXE filename is `LiveReloadWebServer` which is different from the Dotnet Tool's `LiveReloadServer` so they can exist side by side without conflict.

> Any of the following examples use `LiveReloadServer`, and you should substitute `LiveReloadServer` with `LiveReloadWebServer` for any non dotnet tool  installations.

### Launching the Web Server
You can use the command line to customize how the server runs. By default files are served out of the current directory on port `5200`, but you can override the `WebRoot` folder.

Use commandline parameters to customize:

```ps
LiveReloadServer "c:/temp/My Web Site" --port 5200 -useSsl -openEditor
```

There are a number of Configuration options available:

```text
Syntax:
-------
LiveReloadServer <path> <options>

--WebRoot                <path>  (current Path if not provided)
--Port                   5200*
--Host                   0.0.0.0*|localhost|custom Ip - 0.0.0.0 allows external access
--UseSsl                 True|False*
--UseRazor          	 True|False*

--UseLiveReload          True*|False
--Extensions             ".cshtml,.css,.js,.htm,.html,.ts"*
--DefaultFiles           "index.html,default.htm"*

--ShowUrls               True|False*
--OpenBrowser            True*|False
--BrowserUrl             optional startup url (site relative or absolute)
--OpenEditor             True|False*
--EditorLaunchCommand    "code \"%1\""* (Win) or 
                         "open -a \"Visual Studio Code\" \"%1\""* (Mac)
--DetailedErrors         True*|False
--Environment            Production*|Development

Razor Pages:
------------
--UseRazor               True|False*

Markdown Options:
-----------------
--UseMarkdown           True|False*
--CopyMarkdownResources True|False*
--MarkdownTemplate      ~/markdown-themes/__MarkdownTestmplatePage.cshtml*
--MarkdownTheme         github*|dharkan|medium|blackout|westwind
--MarkdownSyntaxTheme   github*|vs2015|vs|monokai|monokai-sublime|twilight

Configuration options can be specified in:

* Command Line options as shown above
* Logical Command Line Flags for true can be set like: -UseSsl or -UseRazor or -OpenBrowser
* Environment Variables with 'LIVERELOADSERVER_' prefix. Example: 'LIVERELOADSERVER_PORT'

Examples:
---------
LiveReloadServer --WebRoot "c:\temp\My Site" --port 5500 -useSsl -useRazor --openBrowser false

$env:LiveReloadServer_Port 5500
$env:LiveReloadServer_WebRoot c:\mySites\Site1\Web
LiveReloadServer
```

You can also use Environment variables to set these save options by using a `LiveReloadServer_` prefix:

```ps
$env:LiveReloadServer_Port 5500
LiveReload
```

For more info please go to the GitHub documentation:


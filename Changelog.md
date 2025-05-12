# Live Reload Server Change Log

### Version 1.7.5

* **Improved for `<markdown>` Blocks in Razor Pages**  
`<markdown>` block parsing now works without requiring the `-useMarkdown` command line switch. 

* **External Editor Command Line**  
Fix how the VS Code default is setup by directly executing `code.exe` rather than using the shell proxy which would often leave an open command window behind.

### Version 1.7

<small>March 18th, 2025</small>

* **Add Support for NuGet Packages**  
Added support for NuGet packages that can be added in `/PrivateBin/NugetPackages.json` configuration. You can specify package Ids and provide multiple package sources.

### Version 1.6

<small>March 15th, 2025</small>

* **Add support for Virtual Path Hosting**  
You can now host a site with an optional virtual path so that you can use relative base paths that simulate virtual directories on a server. Useful for example if you use `https://mySite.com/docs/` folder for a documentation and you want to duplicate that locally. You can with `--VirtualPath /docs/` now.

* **-RegisterExplorer -UnregisterExplorer to Open Web Site**  
You can use these commands to add and remove Explorer integration that allows the current user to open a Web site from the Explorer context menu. Sites are opened with the default settings (defined in the startup folder's `LiveReloadWebServerConfiguration.json`) using the folder as the parameter.

* **-OpenSettings to easily access Default Configuration Settings**  
You can now use the `-OpenSettings` command line switch to open an editor with the Json configuration file opened.

* **Automatic Available Host Port Detection**  
LiveReloadServer now defaults to a port number of 0 which scans for open ports starting with port 5200 (the old default). If the port is in use it finds the next available port and uses that. You can still force a specific port using `--port 5201` switch.

* **Update to .NET 9.0**  
Updated the server to use the .NET 9.0 Runtime.


### Version 1.3
<small>November 20th, 2023</small>

* **Update to .NET 8.0 Runtime**  
Update to the latest .NET Runtime LTS release upon RTM release. This .NET release significantly improves startup times and smaller improvements in overall processing performance. 


* **Fix: FolderNotFoundFallbackPath Handling**  
Fix `FolderNotFoundFallbackPath` which wasn't working properly on `404` errors when no Razor pages are in use. Moved the handler into it's own endpoint processing logic so it always gets applied when set now.

* **Update Markdown Helper 3.7.0**  
Bring in latest Markdown helpers from `Westwind.AspNetCore.Markdown` including latest updates to Markdig.


### Version 1.2
<small>November 12th, 2022</small>

* **Update to .NET 7.0 Runtime**  
Update to the latest .NET Runtime after RTM release. Overall small performance improvements over 6.0.


### Version 1.1 
<small>November 11th, 2021</small>

* **Add Support for .NET 6.0**  
v1.1 and later now runs on .NET Core 6.0 rather than 5.0 previously. The dotnet tool should automatically use 1.0.x for .NET 5.0 SDKs and .NET 6.0 for 1.1+. The self-contained project and Chocolatey packages now run .NET 6 and the hosted version depends on .NET 6.0 now. .NET 6.0 improves overall performance and memory usage.

* **Add `BrowserUrl` Configuration Switch**  
You can now optionally specify an explicit startup URL when launching LiveReloadServer. The URL specified in `BrowserUrl` can either be absolute (`https://localhost:5200/test.html` or a relative site path (ie. `/test.html` or `/subfolder/test.thml`). If not specified the root URL site URL is used.

* **Fix -OpenEditor Option for Mac**  
Fix -OpenEditor on Mac for opening VS Code by default in the WebRoot folder if specified. Find a x-plat way to open both on Windows and Mac (and likely also on Linux).

* **Better Handling of LiveReload when Pages are manually Refreshed**  
Added better support for disconnecting WebSocket when a page is navigated away from or refreshed. In those scenarios sometimes the WebSocket would still fire after the page has already unloaded resulting in multiple immediate requests on a reloaded page. This should remove some of the request clutter that can 'pile' up, especially when many windows are open at the same time with the same monitored site.

### Version 1.0
<small>March 8, 2021</small>
* **Environment Variable Path Fix ups**  
The WebRoot path can now include Environment variables as well as root path `~` markers to resolve to a fully qualified launch path.

* **Improved Server Hosting Functionality**  
Provide default `web.config` for IIS hosting and updated documentation to allow you to host a shared instance of this server on a Web site. Great for hosting simple 'mostly' static sites that also need a few dynamic features, without having to install and deploy a full ASP.NET Core application. You can install one 'runtime' and use it for many sites.

* **Add `-openEditor` and `--editorLaunchCommand` Config Options**  
The new `-openEditor` command allows opening an editor when a site is opened. This is useful if you just start working on a site and you can both launch the site and the editor at the same time. By default VS Code is launched via `code "%1"` which can be overridden with a custom editor launch command.

* **Improved Retry Handling in Browser if Server is Stopped**  
Retry attempts are minimized if the server is shut down which will reduce initial hits if the server is restarted after a break which should improve startup speed.

* **Externalized Script**  
Remove inline script from rendered HTML content pages and use `<script>` tag to pull in the JavaScript code to reload the page from script.

### Version 0.2.16

* **Add Support for DeveloperErrorPage**  
Since this server is meant primarily for development scenarios, we've added support for the `--DetailedErrors` which when set uses the default ASP.NET Developer error page which provides lots of error detail. Note: You have to provide an `/error.cshtml` page with specific code in the file for this to work ([see docs](https://github.com/RickStrahl/LiveReloadServer##developer-error-page)).

* **Break out LiveReloadServer into a separate GitHub Project**  
Removed the LiveReloadServer project from the Westwind.AspNetCore.LiveReload GitHub project and moved into its own separate GitHub repository.

* **Add --Host Configuration Value**  
You can now specify the host IP Address or domain to bind the server to. Previously the server was bound to localhost which didn't allow for external network access. Using `--Host` as a parameter or configuration value you can now specify `0.0.0.0` for example to bind to all IP addresses and allow external access. The default is still `localhost` but you can now explicitly add external access via `--Host 0.0.0.0` or using a specific IP Address to bind to.

### Version 0.2.4

* **Add --Host Configuration Value**  
You can now specify the host IP Address or domain to bind the server to. Previously the server was bound to localhost which didn't allow for external network access. Using `--Host` as a parameter or configuration value you can now specify `0.0.0.0` for example to bind to all IP addresses and allow external access. The default is still `localhost` but you can now explicitly add external access via `--Host 0.0.0.0` or using a specific IP Address to bind to.

### Version 0.2.3

* **Add Blazor Viewing Support**  
Added support for reading custom extensions like the required `.dll` extension for .NET assemblies in Blazor applications. Also added Refresh Fallback support for client side navigation URLs by allowing to redirect to the `/index.html` page on the client for server side refreshes.

### Version 0.2.2

* **Add Markdown File Support**  
Added support for optionally serving Markdown files as HTML from the local site. Markdown files are loaded as `.md`,`.markdown`, `.mkdown` or as extensionless URLs from the Web site and can participate in Live Reload functionality.

* **Add Console Application Icon** 
Added Console Application icon so application is easier to identify in the Task list and when running on the Desktop. 

* **Update the Sample Application**  
Updated the .NET Core 3.1 Sample application to properly display reference links. Add Markdown Example.

* **Fix: Server Timeout not respected**   
The server timeout was not respected previously and has been fixed to properly wait for the configured period before refreshing the browser instance.

* **Fix: Command Line Parsing for Logical Switches**  
Fix issue with logical switches like `-UseSSL` which were not properly working when configuration was present in the configuration file. Settings of the command line now properly override configuration setting in the config file.
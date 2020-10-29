# Live Reload Server Change Log

### Version 0.2.16

* **Add Support for DeveloperErrorPage**  
Since this server is meant primarily for development scenarios, we've added support for the `--DetailedErrors` which when set uses the default ASP.NET Developer error page which provides lots of error detail. Note: You have to provide an `/error.cshtml` page with specific code in the file for this to work ([see docs](https://github.com/RickStrahl/LiveReloadServer##developer-error-page)).

* **Break out LiveReloadServer into a separate Github Project**  
Removed the LiveReloadServer project from the Westwind.AspNetCore.LiveReload Github project and moved into its own separate GitHub repository.

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
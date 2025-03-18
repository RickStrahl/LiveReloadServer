using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using LiveReloadServer;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Westwind.Utilities;
using Westwind.Utilities.Properties;

public class NuGetPackageLoader
{
    private readonly string _packagesFolder;

    public NuGetPackageLoader(string packagesFolder)
    {
        _packagesFolder = packagesFolder;
    }

    public async Task LoadPackageAsync(string packageId, string version, IMvcBuilder builder, 
                IList<string> packageSources = null,
                IList<string> sucessPackages = null,
                IList<string> failedPackages = null)
    {
        sucessPackages = sucessPackages ?? new List<string>();
        failedPackages = failedPackages ?? new List<string>();

        var logger = NullLogger.Instance;
        var cache = new SourceCacheContext();
        var repositories = Repository.Provider.GetCoreV3();


        var packageVersion = new NuGetVersion(version);
        var packagePath = Path.Combine(_packagesFolder, packageId, version);
        Directory.CreateDirectory(packagePath);

        if (packageSources ==  null || packageSources.Count < 1)
        {
            packageSources = ["https://api.nuget.org/v3/index.json"];
        }

        using var packageStream = new MemoryStream();

        FindPackageByIdResource resource = null;

        foreach (var source in packageSources)
        {

            if (!source.StartsWith("http"))
            {
                // local packages
                var sourceRepository = Repository.Factory.GetCoreV3("d:\\projects\\Nuget");
                try
                {
                    resource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();
                }
                catch { continue; }
            }
            else
            {
                // only packages
                var packageSource = new PackageSource(source);
                var sourceRepository = new SourceRepository(packageSource, repositories);
                try
                {
                    resource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();
                }
                catch { continue; }
            }
            if (await resource.CopyNupkgToStreamAsync(packageId, packageVersion, packageStream, cache, logger, default))
            {
                packageStream.Seek(0, SeekOrigin.Begin);
                break;
            }
            else
            {
                failedPackages.Add(packageId);
                return;
            }

            resource = null;
        }

        if (packageStream == null || packageStream.Length < 1)
        {
            failedPackages.Add(packageId);
            return;
        }

        var packageReader = new PackageArchiveReader(packageStream);

        // find the highest compatible framework
        string framework = GetTargetFramework(packageReader);

        // load dependencies
        var dependencies = packageReader
                    .GetPackageDependencies()
                    .Where(d => d.TargetFramework.ToString() == framework)
                    .ToList();

        foreach (var packRef in dependencies)
        {
            var pkg = packRef.Packages.FirstOrDefault();
            if (pkg?.Id == null) // framework references?
                continue;
            await LoadPackageAsync(pkg.Id, pkg.VersionRange.OriginalString, builder,packageSources, sucessPackages, failedPackages);
        }



        sucessPackages.Add(packageId);

        var files = packageReader.GetFiles().Where(f => f.Contains("/" + framework + "/"));
        foreach (var file in files)
        {
            var filePath = Path.Combine(packagePath, file.Replace("/", "\\"));
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));  
                try
                {
                    using var fileStream = File.Create(filePath);
                    await packageReader.GetStream(file).CopyToAsync(fileStream);
                }
                catch { /* ignore - most likely the file exists already  */ }
            }


            if (filePath.EndsWith(".dll") && File.Exists(filePath))
            {
                try
                {
                    var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(filePath);
                    if (assembly != null)
                    {
                        builder.AddApplicationPart(assembly);
                        sucessPackages.Add("-- " + assembly);
                    }
                }
                catch(Exception ex)
                {
                    failedPackages.Add("-- " + Path.GetFileName(filePath));
                }
            }
        }
        

    }

    private string GetTargetFramework(PackageArchiveReader packageReader)
    {
        string framework = packageReader.GetReferenceItems().
                                Where(g =>
                                {
                                    string framework = g.TargetFramework.ToString();
                                    if (framework == "net9.0" || framework == "net8.0" || framework == "net6.0" | framework == "net7.0" ||
                                        framework.StartsWith("netstandard"))
                                        return true;
                                    return false;
                                })
                                .OrderByDescending(g => g.TargetFramework.ToString())
                                .Select(g => g.TargetFramework.ToString())
                                .FirstOrDefault();
        return framework;
    }
}

public class NuGetPackageConfiguration
{
    public List<NugetPackageItem> Packages { get; set; } = new();

    public List<string> NugetSources { get; set; } = new() {
        "https://api.nuget.org/v3/index.json"
    };



}



public class NugetPackageItem
{
    public string PackageId { get; set; }
    public string Version { get; set; }
}

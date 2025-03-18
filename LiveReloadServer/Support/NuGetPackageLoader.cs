using System;
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

public class NuGetPackageLoader
{
    private readonly string _packagesFolder;

    public NuGetPackageLoader(string packagesFolder)
    {
        _packagesFolder = packagesFolder;
    }

    public async Task LoadPackageAsync(string packageId, string version, IMvcBuilder builder)
    {
        var logger = NullLogger.Instance;
        var cache = new SourceCacheContext();
        var repositories = Repository.Provider.GetCoreV3();

        var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
        var sourceRepository = new SourceRepository(packageSource, repositories);

        var resource = await sourceRepository.GetResourceAsync<FindPackageByIdResource>();
        var packageVersion = new NuGetVersion(version);

        var packagePath = Path.Combine(_packagesFolder, packageId, version);

        Directory.CreateDirectory(packagePath);
        using var packageStream = new MemoryStream();

        await resource.CopyNupkgToStreamAsync(packageId, packageVersion, packageStream, cache, logger, default);
        packageStream.Seek(0, SeekOrigin.Begin);

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
            if (pkg?.Id == null)
                continue;
            await LoadPackageAsync(pkg.Id, pkg.VersionRange.OriginalString, builder);
        }


        var files = packageReader.GetFiles().Where(f => f.Contains("/" + framework + "/"));
        foreach (var file in files)
        {
            var filePath = Path.Combine(packagePath, file.Replace("/", "\\"));
            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                //FileStream fileStream = null;
                try
                {
                    using var fileStream = File.Create(filePath);
                    await packageReader.GetStream(file).CopyToAsync(fileStream);
                }
                catch { /* ignore - most likely the file exists already  */ }
            }

            if (filePath.EndsWith(".dll") && File.Exists(filePath))
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(filePath);
                Console.WriteLine($"Loaded assembly: {assembly.FullName}");
                
                if (assembly != null)
                    builder.AddApplicationPart(assembly);
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

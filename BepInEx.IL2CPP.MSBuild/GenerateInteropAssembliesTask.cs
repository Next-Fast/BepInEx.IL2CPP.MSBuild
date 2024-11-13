using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx.IL2CPP.MSBuild.Shared;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace BepInEx.IL2CPP.MSBuild
{
    public class GenerateInteropAssembliesTask : AsyncTask
    {
        [Required]
        public required ITaskItem[] Reference { get; set; }

        [Required]
        public required ITaskItem[] Unhollow { get; set; }

        [Output]
        public ITaskItem[]? InteropAssemblies { get; set; }

        public override async Task<bool> ExecuteAsync()
        {
            var assemblies = new Dictionary<string, string>();

            foreach (var reference in Reference)
            {
                var id = reference.GetMetadata("NuGetPackageId");

                const string common = "Il2CppInterop.Common";
                const string generator = "Il2CppInterop.Generator";
                const string cecil = "Mono.Cecil";
                const string iced = "Iced";
                const string logging = "Microsoft.Extensions.Logging.Abstractions";

                if (id is not (common or generator or cecil or iced or logging)) continue;
                var dllPath = reference.ItemSpec;
                assemblies.Add(reference.GetMetadata("Filename"), dllPath);
            }

            if (!assemblies.Any())
            {
                Log.LogError("No Il2CppInterop found, make sure you referenced BepInEx");
                return false;
            }

            var il2CppInteropVersion = Reference.Single(x => x.GetMetadata("NuGetPackageId") == "Il2CppInterop.Common").GetMetadata("NuGetPackageVersion");

            var packagePath = Path.GetDirectoryName(typeof(GenerateInteropAssembliesTask).Assembly.Location) ?? throw new InvalidOperationException();
            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                var assemblyName = new AssemblyName(args.Name);

                var existingAssembly = AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(a => a.GetName().Name == assemblyName.Name);
                if (existingAssembly != null)
                {
                    Log.LogMessage("Passing " + existingAssembly + " for " + assemblyName);
                    return existingAssembly;
                }

                if (assemblies.TryGetValue(assemblyName.Name, out var path))
                {
                    Log.LogMessage("Loading " + path);
                    return Assembly.LoadFrom(path);
                }

                var packagedPath = Path.Combine(packagePath, assemblyName.Name + ".dll");
                if (!File.Exists(packagedPath)) return null;
                Log.LogMessage("Loading " + packagedPath);
                // LoadFile here is used on purpose as a workaround to avoid double loading BepInEx.IL2CPP.MSBuild.Shared
                return Assembly.LoadFile(packagedPath);

            };

            var interopAssemblies = new List<ITaskItem>();

            var proxyAssemblyGenerator = new Il2CppInteropManagerWrapper(Log);

            foreach (var gameLibsPackage in Unhollow.Select(taskItem => new GameLibsPackage(taskItem)))
            {
                var path = await proxyAssemblyGenerator.GenerateAsync(gameLibsPackage, il2CppInteropVersion);

                foreach (var file in Directory.GetFiles(path, "*.dll"))
                {
                    if (Path.GetFileName(file) == "netstandard.dll")
                    {
                        continue;
                    }

                    var taskItem = new TaskItem(file);
                    taskItem.SetMetadata("PackageId", gameLibsPackage.Id);

                    interopAssemblies.Add(taskItem);
                }
            }

            InteropAssemblies = interopAssemblies.ToArray();

            return true;
        }
    }
}

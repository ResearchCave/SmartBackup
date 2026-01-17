using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using SmartBackup.Common;

namespace SmartBackup.Services
{
    public sealed class PluginManager
    {
        public sealed class Plugin
        {
            public required string TypeKey { get; init; }
            public required Type ModuleType { get; init; }
            public Type? ConfigurationType { get; init; }

            // backward compatibility
            public Type BackupModule => ModuleType;
            public Type? BackupInfo => ConfigurationType;
        }

        private readonly Dictionary<string, Plugin> _modules =
            new(StringComparer.InvariantCultureIgnoreCase);

        public IReadOnlyDictionary<string, Plugin> Modules => _modules;

        public string PluginsPath { get; }

        public PluginManager(string? pluginsPath = null)
        {
            PluginsPath = pluginsPath ??
                Path.Combine(
                    Path.GetDirectoryName(Environment.ProcessPath)!,
                    "plugins");
        }

        // ----------------------------
        // Public API (legacy safe)
        // ----------------------------

        public void Init() => Load();

        public Type? GetBackupModule(string typeKey)
        {
            return _modules.TryGetValue(typeKey, out var plugin)
                ? plugin.ModuleType
                : null;
        }

        public T Create<T>(string typeKey, IServiceProvider sp) where T : class
        {
            if (!_modules.TryGetValue(typeKey, out var plugin))
                throw new KeyNotFoundException($"Plugin not found: {typeKey}");

            return (T)ActivatorUtilities.CreateInstance(sp, plugin.ModuleType);
        }

        // ----------------------------
        // Core logic
        // ----------------------------

        public void Load()
        {
            _modules.Clear();
#if DEBUG

            // 1) FORCE-LOAD plugin assemblies from APPLICATION FOLDER
            LoadAssembliesFromFolder(AppContext.BaseDirectory);
#endif

            // 2) DISCOVER from all loaded assemblies (built-in + app folder)
   

            // 3) LOAD external plugins (/plugins folder)
            if (Directory.Exists(PluginsPath))
            {
                LoadAssembliesFromFolder(PluginsPath);
            }
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                DiscoverPlugins(asm);
            }

            Log.Information("Total plugins loaded: {Count}", _modules.Count);
        }

        // ----------------------------
        // Internals
        // ----------------------------

        private void LoadAssembliesFromFolder(string folder)
        {
            foreach (var dll in Directory.EnumerateFiles(folder, "*.dll"))
            {
                try
                {
                    var name = AssemblyName.GetAssemblyName(dll);

                    // avoid duplicate loads
                    if (AppDomain.CurrentDomain.GetAssemblies()
                        .Any(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), name)))
                        continue;

                    // limit scope (important)
                  //  if (!name.Name!.StartsWith("SmartBackup", StringComparison.OrdinalIgnoreCase))
                  //      continue;

                    AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
                }
                catch (BadImageFormatException ex)
                {
                    Log.Warning( "Skipping non-.NET DLL: {0} ({1})", dll, ex.Message);
                }
                catch
                {
                    // ignore optional plugins
                }
            }
        }

        private void DiscoverPlugins(Assembly asm)
        {
            foreach (var type in GetTypesSafe(asm))
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                var attr = type.GetCustomAttribute<BackupModuleAttribute>();
                if (attr == null)
                    continue;

                if (!typeof(IBackupOperation).IsAssignableFrom(type))
                    continue;

                var plugin = new Plugin
                {
                    TypeKey = attr.Type,
                    ModuleType = type,
                    ConfigurationType = attr.ConfigurationType
                };

                if (_modules.TryAdd(plugin.TypeKey, plugin))
                {
                    Log.Information(
                        "Registered plugin '{Key}' from {Assembly}",
                        plugin.TypeKey,
                        asm.GetName().Name);
                }
            }
        }

        private static IEnumerable<Type> GetTypesSafe(Assembly asm)
        {
            try
            {
                return asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null)!;
            }
        }
    }
}

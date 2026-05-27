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
        private readonly List<AssemblyLoadContext> _pluginContexts = new();

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
            _pluginContexts.Clear();
#if DEBUG

            // 1) FORCE-LOAD plugin assemblies from APPLICATION FOLDER
            LoadAssembliesFromAppFolder(AppContext.BaseDirectory);
#endif

            // 2) LOAD external plugins (/plugins folder)
            if (Directory.Exists(PluginsPath))
            {
                LoadPluginAssemblies(PluginsPath);
            }

            // 3) DISCOVER from all loaded assemblies (built-in + plugins)
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                DiscoverPlugins(asm);
            }

            Log.Information("Total plugins loaded: {Count}", _modules.Count);
        }

        // ----------------------------
        // Internals
        // ----------------------------

        private void LoadAssembliesFromAppFolder(string folder)
        {
            foreach (var dll in Directory.EnumerateFiles(folder, "SmartBackup*.dll"))
            {
                try
                {
                    var name = AssemblyName.GetAssemblyName(dll);

                    // avoid duplicate loads
                    if (AppDomain.CurrentDomain.GetAssemblies()
                        .Any(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), name)))
                        continue;

                    AssemblyLoadContext.Default.LoadFromAssemblyPath(dll);
                }
                catch (BadImageFormatException ex)
                {
                    Log.Warning("Skipping non-.NET DLL: {0} ({1})", dll, ex.Message);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Unable to load assembly from app folder: {Path}", dll);
                }
            }
        }

        private void LoadPluginAssemblies(string pluginsPath)
        {
            foreach (var pluginDir in Directory.EnumerateDirectories(pluginsPath))
            {
                foreach (var pluginDll in GetPluginEntryAssemblies(pluginDir))
                {
                    LoadPluginAssembly(pluginDll);
                }
            }

            foreach (var pluginDll in Directory.EnumerateFiles(pluginsPath, "SmartBackup*.dll"))
            {
                LoadPluginAssembly(pluginDll);
            }
        }

        private static IEnumerable<string> GetPluginEntryAssemblies(string pluginDir)
        {
            var preferredDll = Path.Combine(pluginDir, Path.GetFileName(pluginDir) + ".dll");
            if (File.Exists(preferredDll))
            {
                yield return preferredDll;
                yield break;
            }

            foreach (var dll in Directory.EnumerateFiles(pluginDir, "SmartBackup*.dll"))
            {
                var name = Path.GetFileNameWithoutExtension(dll);
                if (!name.Equals("SmartBackup.Common", StringComparison.OrdinalIgnoreCase))
                    yield return dll;
            }
        }

        private void LoadPluginAssembly(string pluginDll)
        {
            try
            {
                var name = AssemblyName.GetAssemblyName(pluginDll);

                if (AppDomain.CurrentDomain.GetAssemblies()
                    .Any(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), name)))
                    return;

                var loadContext = new PluginLoadContext(pluginDll);
                _pluginContexts.Add(loadContext);
                loadContext.LoadFromAssemblyPath(pluginDll);
            }
            catch (BadImageFormatException ex)
            {
                Log.Warning("Skipping non-.NET plugin DLL: {0} ({1})", pluginDll, ex.Message);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Unable to load plugin assembly: {Path}", pluginDll);
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
                foreach (var loaderException in ex.LoaderExceptions.Where(e => e != null))
                {
                    Log.Warning(loaderException, "Unable to load one or more types from {Assembly}", asm.FullName);
                }

                return ex.Types.Where(t => t != null)!;
            }
        }

        private sealed class PluginLoadContext : AssemblyLoadContext
        {
            private readonly AssemblyDependencyResolver _resolver;

            public PluginLoadContext(string pluginPath)
                : base(Path.GetFileNameWithoutExtension(pluginPath), isCollectible: false)
            {
                _resolver = new AssemblyDependencyResolver(pluginPath);
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                var sharedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => AssemblyName.ReferenceMatchesDefinition(a.GetName(), assemblyName));

                if (sharedAssembly != null)
                    return sharedAssembly;

                var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
                return assemblyPath != null ? LoadFromAssemblyPath(assemblyPath) : null;
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
                return libraryPath != null ? LoadUnmanagedDllFromPath(libraryPath) : IntPtr.Zero;
            }
        }
    }
}

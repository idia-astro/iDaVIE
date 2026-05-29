// SPDX-License-Identifier: LGPL-3.0-or-later
// NativePluginLoader — ST2 Infrastructure. Reflection-based P/Invoke delegate
// binding for the CFITSIO / Starlink-AST / DataAnalysis native DLLs.
// Replaces Assets/Scripts/PluginInterface/NativePluginLoader.cs (271 LOC).
//
// Realises no cross-team contract; invoked once at startup by KernelCompositionRoot
// before PluginRegistry.Register{FitsReaderPlugin,WcsTransformPlugin,DataAnalysisPlugin}.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace iDaVIE.Data
{
    internal static class NativePluginLoader
    {
        private static readonly Dictionary<string, IntPtr> LoadedLibraries =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Locates and loads the per-platform plug-in DLLs from the
        /// `Plugins/` directory, binding every P/Invoke delegate by reflection.</summary>
        public static void LoadAll() => LoadAll(null);

        public static void LoadAll(string? pluginDirectory)
        {
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetLoadableTypes))
            {
                var pluginName = GetPluginName(type);
                if (string.IsNullOrWhiteSpace(pluginName))
                    continue;

                if (!TryLoadLibrary(pluginName, pluginDirectory, out var handle))
                    continue;

                BindPluginFunctions(type, handle);
            }
        }

        public static void UnloadAll()
        {
            foreach (var handle in LoadedLibraries.Values)
                NativeLibrary.Free(handle);
            LoadedLibraries.Clear();
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.OfType<Type>();
            }
        }

        private static string? GetPluginName(MemberInfo type)
        {
            var attribute = type.GetCustomAttributes(inherit: true)
                .FirstOrDefault(attr => attr.GetType().Name == nameof(PluginAttr));
            return attribute == null ? null : ReadStringProperty(attribute, "PluginName", "pluginName");
        }

        private static string? GetFunctionName(MemberInfo field)
        {
            var attribute = field.GetCustomAttributes(inherit: true)
                .FirstOrDefault(attr => attr.GetType().Name == nameof(PluginFunctionAttr));
            return attribute == null ? null : ReadStringProperty(attribute, "FunctionName", "functionName");
        }

        private static string? ReadStringProperty(object attribute, params string[] names)
        {
            var type = attribute.GetType();
            foreach (var name in names)
            {
                var property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property?.GetValue(attribute) is string propertyValue)
                    return propertyValue;

                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field?.GetValue(attribute) is string fieldValue)
                    return fieldValue;
            }

            return null;
        }

        private static bool TryLoadLibrary(string pluginName, string? pluginDirectory, out IntPtr handle)
        {
            foreach (var candidate in LibraryCandidates(pluginName, pluginDirectory))
            {
                if (LoadedLibraries.TryGetValue(candidate, out handle))
                    return true;

                if (NativeLibrary.TryLoad(candidate, out handle))
                {
                    LoadedLibraries[candidate] = handle;
                    return true;
                }
            }

            handle = IntPtr.Zero;
            return false;
        }

        private static IEnumerable<string> LibraryCandidates(string pluginName, string? pluginDirectory)
        {
            if (Path.IsPathRooted(pluginName))
            {
                yield return pluginName;
                yield break;
            }

            var fileNames = PlatformFileNames(pluginName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var directories = new[]
            {
                pluginDirectory,
                Path.Combine(AppContext.BaseDirectory, "Plugins"),
                AppContext.BaseDirectory
            }.Where(path => !string.IsNullOrWhiteSpace(path)).Cast<string>();

            foreach (var directory in directories)
            {
                foreach (var fileName in fileNames)
                    yield return Path.Combine(directory, fileName);
            }

            foreach (var fileName in fileNames)
                yield return fileName;
        }

        private static IEnumerable<string> PlatformFileNames(string pluginName)
        {
            yield return pluginName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return pluginName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    ? pluginName
                    : pluginName + ".dll";
                yield break;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var bare = pluginName.EndsWith(".dylib", StringComparison.OrdinalIgnoreCase)
                    ? pluginName.Substring(0, pluginName.Length - ".dylib".Length)
                    : pluginName;
                yield return bare + ".dylib";
                yield return bare.StartsWith("lib", StringComparison.OrdinalIgnoreCase)
                    ? bare + ".dylib"
                    : "lib" + bare + ".dylib";
                yield break;
            }

            var linuxBare = pluginName.EndsWith(".so", StringComparison.OrdinalIgnoreCase)
                ? pluginName.Substring(0, pluginName.Length - ".so".Length)
                : pluginName;
            yield return linuxBare + ".so";
            yield return linuxBare.StartsWith("lib", StringComparison.OrdinalIgnoreCase)
                ? linuxBare + ".so"
                : "lib" + linuxBare + ".so";
        }

        private static void BindPluginFunctions(Type type, IntPtr handle)
        {
            var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                var functionName = GetFunctionName(field);
                if (string.IsNullOrWhiteSpace(functionName))
                    continue;
                if (!typeof(Delegate).IsAssignableFrom(field.FieldType))
                    continue;
                if (!NativeLibrary.TryGetExport(handle, functionName, out var pointer) || pointer == IntPtr.Zero)
                    continue;

                var function = Marshal.GetDelegateForFunctionPointer(pointer, field.FieldType);
                field.SetValue(null, function);
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    internal sealed class PluginAttr : Attribute
    {
        public PluginAttr(string pluginName) => PluginName = pluginName;
        public string PluginName { get; }
        public string pluginName => PluginName;
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    internal sealed class PluginFunctionAttr : Attribute
    {
        public PluginFunctionAttr(string functionName) => FunctionName = functionName;
        public string FunctionName { get; }
        public string functionName => FunctionName;
    }
}

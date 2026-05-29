// SPDX-License-Identifier: LGPL-3.0-or-later
// PluginRegistry — realises IPluginRegistry (Kernel/Contracts/IPluginRegistry.cs).
// Loaded once at startup; held by KernelCompositionRoot.

using System;
using System.Collections.Generic;
using iDaVIE.Kernel.Contracts;

namespace iDaVIE.Kernel
{
    internal sealed class PluginRegistry : IPluginRegistry
    {
        private readonly Dictionary<Type, object> _plugins = new();

        /// <summary>Registers a realisation of contract type T.</summary>
        public void Register<T>(T plugin) where T : class
            => RegisterPlugin(plugin);

        public void RegisterPlugin<T>(T plugin) where T : class
        {
            if (plugin == null)
                throw new ArgumentNullException(nameof(plugin));

            var contractType = typeof(T);
            if (_plugins.ContainsKey(contractType))
                throw new InvalidOperationException(
                    $"A plug-in is already registered for contract '{contractType.FullName}'.");

            _plugins.Add(contractType, plugin);
        }

        public T GetPlugin<T>() where T : class
        {
            if (TryGetPlugin<T>(out var plugin))
                return plugin;
            throw new PluginNotFoundException(typeof(T));
        }

        public bool TryGetPlugin<T>(out T plugin) where T : class
        {
            if (_plugins.TryGetValue(typeof(T), out var value) && value is T typed)
            {
                plugin = typed;
                return true;
            }

            plugin = null!;
            return false;
        }

        public bool IsRegistered<T>() where T : class
            => _plugins.ContainsKey(typeof(T));
    }
}

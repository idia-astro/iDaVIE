// SPDX-License-Identifier: LGPL-3.0-or-later
// PluginRegistry — realises IPluginRegistry (Kernel/Contracts/IPluginRegistry.cs).
// Loaded once at startup; held by KernelCompositionRoot.

using System;
using iDaVIE.Kernel.Contracts;

namespace iDaVIE.Kernel
{
    internal sealed class PluginRegistry : IPluginRegistry
    {
        /// <summary>Registers a realisation of contract type T.</summary>
        public void Register<T>(T plugin) where T : class
            => throw new NotImplementedException();

        public T GetPlugin<T>() where T : class
            => throw new NotImplementedException();

        public bool TryGetPlugin<T>(out T plugin) where T : class
            => throw new NotImplementedException();
    }
}

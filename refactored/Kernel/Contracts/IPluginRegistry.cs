// SPDX-License-Identifier: LGPL-3.0-or-later
// IPluginRegistry — ST1 cross-team contract. Realised by Kernel/PluginRegistry.cs.

namespace iDaVIE.Kernel.Contracts
{
    /// <summary>Service locator at the kernel boundary; the only legal way to
    /// reach an ST2 plug-in concrete from another layer.</summary>
    public interface IPluginRegistry
    {
        /// <summary>Returns the registered plug-in of the requested contract type,
        /// or throws InvalidOperationException if no plug-in realises it.</summary>
        T GetPlugin<T>() where T : class;

        bool TryGetPlugin<T>(out T plugin) where T : class;
    }
}

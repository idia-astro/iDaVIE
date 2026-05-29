// SPDX-License-Identifier: LGPL-3.0-or-later
// NativePluginLoader — ST2 Infrastructure. Reflection-based P/Invoke delegate
// binding for the CFITSIO / Starlink-AST / DataAnalysis native DLLs.
// Replaces Assets/Scripts/PluginInterface/NativePluginLoader.cs (271 LOC).
//
// Realises no cross-team contract; invoked once at startup by KernelCompositionRoot
// before PluginRegistry.Register{FitsReaderPlugin,WcsTransformPlugin,DataAnalysisPlugin}.

namespace iDaVIE.Data
{
    internal static class NativePluginLoader
    {
        /// <summary>Locates and loads the per-platform plug-in DLLs from the
        /// `Plugins/` directory, binding every P/Invoke delegate by reflection.</summary>
        public static void LoadAll() => throw new System.NotImplementedException();
    }
}

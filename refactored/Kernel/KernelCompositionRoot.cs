// SPDX-License-Identifier: LGPL-3.0-or-later
// KernelCompositionRoot — sole `new()` site for cross-layer concretes
// (global_model.md §1 ST1). Wires PluginRegistry, IConfig, IVolumeRegistry,
// every ST3/ST4/ST5/ST6/ST7 service, and every `Inject(...)` call in the scene.
//
// Holds no domain state. Its single Bootstrap() entry point is invoked once at
// application start-up after Unity's scene graph is loaded.

using iDaVIE.Data;
using iDaVIE.Data.Contracts;
using iDaVIE.Features;
using iDaVIE.Kernel.Contracts;
using iDaVIE.Kernel.Contracts.Plugins;

namespace iDaVIE.Kernel
{
    public sealed class KernelCompositionRoot
    {
        public IConfig          Config          { get; private set; }
        public IPluginRegistry  PluginRegistry  { get; private set; }
        public IVolumeRegistry  VolumeRegistry  { get; private set; }
        public IVolumeLoader    VolumeLoader    { get; private set; }
        public ILogSink         LogSink         { get; private set; }
        public IBenchmarkHarness BenchmarkHarness { get; private set; }
        public IDesktopShell?   DesktopShell    { get; private set; }

        /// <summary>Loads Config, populates PluginRegistry, constructs every cross-layer
        /// service and calls Inject() on every scene-mounted MonoBehaviour in dependency
        /// order: ST1 → ST2 → ST3 → ST4 → ST5 → ST6 → ST7.</summary>
        public void Bootstrap(string configJsonPath)
        {
            var config = iDaVIE.Kernel.Contracts.Config.LoadFromJson(configJsonPath);
            var log = new DebugLogSink(config.LogRingCapacity);
            var registry = new PluginRegistry();
            var volumeRegistry = new VolumeRegistry();

            NativePluginLoader.LoadAll();

            var fits = new FitsReaderPlugin();
            var wcs = new WcsTransformPlugin();
            var analysis = new DataAnalysisPlugin();
            var mask = new MaskEditService();

            registry.RegisterPlugin<IFitsPlugin>(fits);
            registry.RegisterPlugin<IFitsBinaryTableSource>(fits);

            registry.RegisterPlugin<IWcsPlugin>(wcs);
            registry.RegisterPlugin<IWcsMapping>(wcs);
            registry.RegisterPlugin<ICoordinateTransformer>(wcs);

            registry.RegisterPlugin<ISourceStatsProvider>(analysis);
            registry.RegisterPlugin<IDataAnalysisPlugin>(analysis);
            registry.RegisterPlugin<IMaskEditState>(mask);
            registry.RegisterPlugin<IMaskMutationService>(mask);
            registry.RegisterPlugin<IBrushStrokeHistory>(mask);
            registry.RegisterPlugin<IMaskStateCapture>(mask);

            Config = config;
            PluginRegistry = registry;
            VolumeRegistry = volumeRegistry;
            LogSink = log;
            VolumeLoader = new VolumeLoader(registry, volumeRegistry, log);
            BenchmarkHarness = new BenchmarkHarness(log);

            log.LogInfo(nameof(KernelCompositionRoot), "Kernel services bootstrapped.");
        }
    }
}

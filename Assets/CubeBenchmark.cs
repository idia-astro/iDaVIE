using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Native private-bytes (pagefile usage) for the current process (Windows).
/// Captures Unity + native plugins and is usually more stable than managed-only metrics.
/// </summary>
public static class NativeMemory
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_MEMORY_COUNTERS
    {
        public uint cb;
        public uint PageFaultCount;
        public ulong PeakWorkingSetSize;
        public ulong WorkingSetSize;
        public ulong QuotaPeakPagedPoolUsage;
        public ulong QuotaPagedPoolUsage;
        public ulong QuotaPeakNonPagedPoolUsage;
        public ulong QuotaNonPagedPoolUsage;
        public ulong PagefileUsage;
        public ulong PeakPagefileUsage;
    }

    [DllImport("psapi.dll", SetLastError = true)]
    static extern bool GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS counters, uint size);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetCurrentProcess();

    public static ulong GetPrivateBytes()
    {
        PROCESS_MEMORY_COUNTERS counters;
        if (GetProcessMemoryInfo(GetCurrentProcess(), out counters,
            (uint)Marshal.SizeOf(typeof(PROCESS_MEMORY_COUNTERS))))
        {
            return counters.PagefileUsage; // "private bytes" style metric including native plugins
        }
        return 0;
    }
}

/// <summary>
/// Minimal, paper-friendly benchmarking:
/// - End-to-end load time (start -> renderer ready)
/// - Peak/Final RAM (private bytes)
/// - Downsampling factors + resulting texture dims
/// - VRAM estimate (data+mask) from downsampled dims
/// - FPS min/median/mean during a standardised interaction (approach + zoom)
///
/// Outputs a compact CSV: benchmark_summary.csv
///
/// Usage (from your loader/orchestrator):
///   benchmark.StartLoadBenchmark(cubeLabel, nx, ny, nz);
///   ... perform load ...
///   benchmark.EndLoadBenchmark(dsx, dsy, dsz);
///   benchmark.BeginFpsBenchmark(loadedCubeTransform); // viewerTransform is assigned in Inspector
/// </summary>
public class CubeBenchmark : MonoBehaviour
{
    [Header("FPS Interaction (One-Phase Translation)")]

    [Tooltip("Cube starts this many meters in front of the camera (camera-local +Z).")]
    public float StartDistance = 2.0f;

    [Tooltip("Cube ends at this distance. 0 means camera reaches cube centre.")]
    public float EndDistance = 0.0f;

    [Tooltip("Duration of the translation in seconds.")]
    public float MoveSeconds = 10.0f;

    [Tooltip("If true, cube is temporarily parented to camera during FPS test.")]
    public bool ParentToCamera = true;

    [Header("Viewer Reference (XR Camera)")]
    [Tooltip("Assign the HMD camera / CenterEye transform here.")]
    [SerializeField] private Transform viewerTransform;

    [Header("CSV Output")]
    [SerializeField] private string summaryFile = "benchmark_summary.csv";

    [Header("Voxel Formats")]
    [Tooltip("Bytes per voxel in the data cube texture (e.g., float = 4, half = 2).")]
    public int BytesPerDataVoxel = 4;

    [Tooltip("Bytes per voxel in the mask cube texture (e.g., ushort/half-int = 2).")]
    public int BytesPerMaskVoxel = 2;

    [Header("FPS Benchmark Protocol")]
    [Tooltip("Phase 1 duration: move cube toward viewer (seconds).")]
    public float ApproachSeconds = 7f;

    [Tooltip("Phase 2 duration: zoom into cube (seconds).")]
    public float ZoomSeconds = 8f;

    [Tooltip("How close the cube moves relative to initial viewer->cube distance (0.0-1.0). Higher = closer.")]
    [Range(0.0f, 1.0f)]
    public float ApproachStrength = 0.6f;

    [Tooltip("Scale multiplier applied during zoom phase.")]
    public float ZoomFactor = 1.25f;

    [Tooltip("Seconds to wait after load ends before starting FPS sampling (lets rendering settle).")]
    public float SettleSecondsBeforeFps = 0.5f;

    [Tooltip("If true, the cube is restored to its original transform after the FPS test.")]
    public bool RestoreCubeTransformAfterFps = true;

    // Load benchmark state
    private Stopwatch sw;
    private long baselineMemory;
    private long peakMemory;
    private string cubeLabel = "Unknown";

    private int voxX, voxY, voxZ;              // original dims
    private int dsX = 1, dsY = 1, dsZ = 1;     // downsample factors
    private long downX, downY, downZ;          // downsampled dims

    private bool loadBenchmarkActive = false;

    // Peak RAM tracker coroutine
    private Coroutine peakRoutine;

    // FPS stats
    private bool fpsDone = false;
    private int fpsSamples = 0;
    private double fpsMin = double.NaN;
    private double fpsMedian = double.NaN;
    private double fpsMean = double.NaN;

    private bool fpsRunning = false;

    private void Awake()
    {
        EnsureSummaryHeader();
    }

    /// <summary>
    /// Call at the start of loading (e.g., when user confirms "Load").
    /// nx/ny/nz are the ORIGINAL cube dims (before any downsampling).
    /// </summary>
    public void StartLoadBenchmark(string cubeName, int nx, int ny, int nz)
    {
        cubeLabel = cubeName;
        voxX = nx; voxY = ny; voxZ = nz;

        // reset FPS stats
        fpsDone = false;
        fpsRunning = false;
        fpsSamples = 0;
        fpsMin = fpsMedian = fpsMean = double.NaN;

        sw = Stopwatch.StartNew();
        baselineMemory = (long)NativeMemory.GetPrivateBytes();
        peakMemory = baselineMemory;

        loadBenchmarkActive = true;

        // Start peak tracker (per-frame while loading)
        if (peakRoutine != null) StopCoroutine(peakRoutine);
        peakRoutine = StartCoroutine(TrackPeakWhileLoading());

        Debug.Log($"[BENCH] Start load: {cubeLabel} ({voxX}x{voxY}x{voxZ})");
    }

    /// <summary>
    /// Call when the renderer is fully initialised and the user can interact.
    /// Provide the ACTUAL downsampling factors used (1 = none).
    /// Writes a single row that includes load metrics and (once finished) FPS metrics.
    /// FPS metrics are filled after BeginFpsBenchmark completes.
    /// </summary>
    public void EndLoadBenchmark(int xDownsampleFactor = 1, int yDownsampleFactor = 1, int zDownsampleFactor = 1)
    {
        if (!loadBenchmarkActive || sw == null)
        {
            Debug.LogWarning("[BENCH] EndLoadBenchmark called but no active benchmark.");
            return;
        }

        dsX = Mathf.Max(1, xDownsampleFactor);
        dsY = Mathf.Max(1, yDownsampleFactor);
        dsZ = Mathf.Max(1, zDownsampleFactor);

        downX = Math.Max(1, voxX / (long)dsX);
        downY = Math.Max(1, voxY / (long)dsY);
        downZ = Math.Max(1, voxZ / (long)dsZ);

        sw.Stop();
        loadBenchmarkActive = false;

        if (peakRoutine != null)
        {
            StopCoroutine(peakRoutine);
            peakRoutine = null;
        }

        // Capture final peak once more
        UpdatePeakRam();

        double loadSec = sw.Elapsed.TotalSeconds;
        double finalRamMb = ((long)NativeMemory.GetPrivateBytes() - baselineMemory) / (1024.0 * 1024.0);
        double peakRamMb = (peakMemory - baselineMemory) / (1024.0 * 1024.0);
        double vramMb = EstimateVramMb(downX, downY, downZ);

        Debug.Log($"[BENCH] Load done: {cubeLabel} | {loadSec:F2}s | PeakRAM {peakRamMb:F0}MB | FinalRAM {finalRamMb:F0}MB | VRAM~ {vramMb:F0}MB | Down {dsX}x{dsY}x{dsZ} -> {downX}x{downY}x{downZ}");

        // Store load results; FPS will be added when FPS benchmark finishes.
        // We write the row at the end of FPS benchmark to keep "one row per trial".
        _pendingLoadSec = loadSec;
        _pendingPeakRamMb = peakRamMb;
        _pendingFinalRamMb = finalRamMb;
        _pendingVramMb = vramMb;
        _pendingHasLoad = true;
    }

    /// <summary>
    /// Start the deterministic FPS benchmark.
    /// Pass the loaded cube transform. The viewer (XR camera) is assigned in the inspector.
    /// When finished, this writes a single CSV row (load + FPS).
    /// </summary>
    public void BeginFpsBenchmark(Transform cubeTransform)
    {
        if (viewerTransform == null)
        {
            Debug.LogWarning("[BENCH] Viewer transform not assigned.");
            return;
        }

        StartCoroutine(FpsProtocolCoroutine(cubeTransform));
    }


    // ---------------------- Internals ----------------------

    // pending load values to write one compact row per trial
    private bool _pendingHasLoad = false;
    private double _pendingLoadSec = double.NaN;
    private double _pendingPeakRamMb = double.NaN;
    private double _pendingFinalRamMb = double.NaN;
    private double _pendingVramMb = double.NaN;

    private IEnumerator TrackPeakWhileLoading()
    {
        while (loadBenchmarkActive)
        {
            UpdatePeakRam();
            yield return null; // once per frame
        }
    }

    private void UpdatePeakRam()
    {
        long cur = (long)NativeMemory.GetPrivateBytes();
        if (cur > peakMemory) peakMemory = cur;
    }

    private double EstimateVramMb(long x, long y, long z)
    {
        double data = x * (double)y * z * BytesPerDataVoxel / (1024.0 * 1024.0);
        double mask = x * (double)y * z * BytesPerMaskVoxel / (1024.0 * 1024.0);
        return data + mask;
    }

    private void EnsureSummaryHeader()
    {
        if (!File.Exists(summaryFile) || new FileInfo(summaryFile).Length == 0)
        {
            string header =
                "Cube," +
                "DimX,DimY,DimZ," +
                "DownX,DownY,DownZ," +
                "DownDimX,DownDimY,DownDimZ," +
                "LoadTime(s),PeakRAM(MB),FinalRAM(MB),VRAM(MB)," +
                "FPS_Median,FPS_Min,FPS_Mean,FPS_Samples\n";
            File.AppendAllText(summaryFile, header);
        }
    }

    private void AppendSummaryRow()
    {
        EnsureSummaryHeader();

        string line =
            $"{cubeLabel}," +
            $"{voxX},{voxY},{voxZ}," +
            $"{dsX},{dsY},{dsZ}," +
            $"{downX},{downY},{downZ}," +
            $"{Fmt(_pendingLoadSec)},{Fmt(_pendingPeakRamMb)},{Fmt(_pendingFinalRamMb)},{Fmt(_pendingVramMb)}," +
            $"{Fmt(fpsMedian)},{Fmt(fpsMin)},{Fmt(fpsMean)},{fpsSamples}\n";

        File.AppendAllText(summaryFile, line);

        Debug.Log($"[BENCH] Wrote CSV row: {summaryFile}");
    }

    private static string Fmt(double v)
    {
        return (double.IsNaN(v) || double.IsInfinity(v)) ? "" : v.ToString("F3");
    }

    private IEnumerator FpsProtocolCoroutine(Transform cubeTransform)
    {
        if (SettleSecondsBeforeFps > 0f)
            yield return new WaitForSecondsRealtime(SettleSecondsBeforeFps);

        // Save original state
        Transform originalParent = cubeTransform.parent;
        Vector3 originalPosition = cubeTransform.position;
        Quaternion originalRotation = cubeTransform.rotation;
        Vector3 originalScale = cubeTransform.localScale;

        if (ParentToCamera)
        {
            cubeTransform.SetParent(viewerTransform, worldPositionStays: false);
            cubeTransform.localRotation = Quaternion.identity;
        }

        Vector3 startLocalPos = new Vector3(0f, 0f, StartDistance);
        Vector3 endLocalPos = new Vector3(0f, 0f, EndDistance);

        cubeTransform.localPosition = startLocalPos;

        List<float> fps = new List<float>(Mathf.CeilToInt(MoveSeconds * 120f));

        float t = 0f;
        float T = Mathf.Max(0.01f, MoveSeconds);

        while (t < T)
        {
            float dt = Time.unscaledDeltaTime;

            if (dt > 0f)
                fps.Add(1f / dt);

            float a = SmoothStep(t / T);
            cubeTransform.localPosition = Vector3.Lerp(startLocalPos, endLocalPos, a);

            t += dt;
            yield return null;
        }

        // Restore original transform
        cubeTransform.SetParent(originalParent, worldPositionStays: true);
        cubeTransform.position = originalPosition;
        cubeTransform.rotation = originalRotation;
        cubeTransform.localScale = originalScale;

        // Compute FPS statistics
        fpsSamples = fps.Count;

        if (fpsSamples > 0)
        {
            fps.Sort();
            fpsMin = fps[0];
            fpsMedian = (fpsSamples % 2 == 1)
                ? fps[fpsSamples / 2]
                : 0.5 * (fps[fpsSamples / 2 - 1] + fps[fpsSamples / 2]);
            fpsMean = fps.Average();
        }

        AppendSummaryRow();
    }


    private static float SmoothStep(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }
}

using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using System.Runtime.InteropServices;
using Debug = UnityEngine.Debug;


/// <summary>
/// Provides methods to access native memory information for Unity.
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
        if (GetProcessMemoryInfo(GetCurrentProcess(), out counters, (uint)Marshal.SizeOf(typeof(PROCESS_MEMORY_COUNTERS))))
        {
            return counters.PagefileUsage; // total private memory including native plugins
        }
        return 0;
    }
}

public class CubeLoadProfiler : MonoBehaviour
{
    private Stopwatch sw;

    /// <summary>
    /// The path to the log file.
    /// </summary>
    private string logFile = "benchmark_milestones.csv";

    /// <summary>
    /// The baseline memory usage at the start of the benchmark.
    /// </summary>
    private long baselineMemory;

    /// <summary>
    /// The peak memory usage during the benchmark.
    /// </summary>
    private long peakMemory;
    private string cubeLabel = "Unknown";

    /// <summary>
    /// The dimensions of the voxel grid.
    /// </summary>
    private int voxelsX, voxelsY, voxelsZ;

    /// <summary>
    /// The number of bytes per data voxel (usually 4 bytes for float)
    /// </summary>
    public int BytesPerDataVoxel { get; private set; } = 4;

    /// <summary>
    /// The number of bytes per mask voxel (usually 2 bytes for half-precision integer)
    /// </summary>
    public int BytesPerMaskVoxel { get; private set; } = 2;

    public bool IsBenchmarking { get; private set; } = false;

    private int numberOfExtraUpdates = 0;

    void Awake()
    {
        if (!File.Exists(logFile))
        {
            File.AppendAllText(logFile, "Cube,Milestone,Time(s),DeltaRAM(MB)\n");
        }
    }

    /// <summary>
    /// Starts the benchmark for loading a volume dataset. Place this call at the beginning of the loading process.
    /// </summary>
    /// <param name="cubeName">A label for the dataset being loaded.</param>
    /// <param name="nx">The number of voxels in the X dimension.</param>
    /// <param name="ny">The number of voxels in the Y dimension.</param>
    /// <param name="nz">The number of voxels in the Z dimension.</param>
    public void StartBenchmark(string cubeName, int nx = 0, int ny = 0, int nz = 0)
    {
        cubeLabel = cubeName;
        voxelsX = nx;
        voxelsY = ny;
        voxelsZ = nz;

        sw = Stopwatch.StartNew();
        baselineMemory = (long)NativeMemory.GetPrivateBytes();
        peakMemory = baselineMemory;
        LogMilestone("Load initiated");
        IsBenchmarking = true;
    }
    /// <summary>
    /// Logs a milestone in the loading process. Place this call right after each significant step in the loading process.
    /// </summary>
    /// <param name="milestone">A label for the milestone being logged.</param>
    public void LogMilestone(string milestone)
    {
        long currentMemory = (long)NativeMemory.GetPrivateBytes();
        long netMemory = currentMemory - baselineMemory;
        peakMemory = Math.Max(peakMemory, currentMemory);

        double deltaMB = netMemory / (1024.0 * 1024.0);
        double elapsed = sw.Elapsed.TotalSeconds;

        if (milestone == "Update")
        {
            milestone += $"_{numberOfExtraUpdates}";
            numberOfExtraUpdates++;
        }

        string line = $"{cubeLabel},{milestone},{elapsed:F3},{deltaMB:F2}\n";
        File.AppendAllText(logFile, line);
    }

    /// <summary>
    /// Ends the benchmark for loading a volume dataset. Place this call at the end of the loading process.
    /// </summary>
    /// <param name="xDownsampleFactor">The downsample factor for the X dimension.</param>
    /// <param name="yDownsampleFactor">The downsample factor for the Y dimension.</param>
    /// <param name="zDownsampleFactor">The downsample factor for the Z dimension.</param>
    public void EndBenchmark(int xDownsampleFactor = 1, int yDownsampleFactor = 1, int zDownsampleFactor = 1)
    {
        sw.Stop();
        IsBenchmarking = false;
        LogMilestone("Finished");

        // Peak and final RAM (CPU)
        double finalMB = ((long)NativeMemory.GetPrivateBytes() - baselineMemory) / (1024.0 * 1024.0);
        double peakMB = (peakMemory - baselineMemory) / (1024.0 * 1024.0);

        // Compute downsampled dimensions
        long downX = voxelsX / xDownsampleFactor;
        long downY = voxelsY / yDownsampleFactor;
        long downZ = voxelsZ / zDownsampleFactor;

        // VRAM calculation based on downsampled textures
        double dataVRAM = downX * downY * downZ * BytesPerDataVoxel / (1024.0 * 1024.0);
        double maskVRAM = downX * downY * downZ * BytesPerMaskVoxel / (1024.0 * 1024.0);
        double totalVRAM = dataVRAM + maskVRAM;

        Debug.Log($"[BENCH] Peak RAM: {peakMB:F2} MB, Final RAM: {finalMB:F2} MB, VRAM: {totalVRAM:F2} MB");

        // Write summary to CSV
        string summaryLine = $"{cubeLabel},{sw.Elapsed.TotalSeconds:F3},{peakMB:F2},{finalMB:F2},{totalVRAM:F2}\n";
        // Add header if file doesn't exist or is empty
        string summaryFile = "benchmark_summary.csv";
        if (!File.Exists(summaryFile) || new FileInfo(summaryFile).Length == 0)
        {
            File.AppendAllText(summaryFile, "Cube,Time(s),PeakRAM(MB),FinalRAM(MB),VRAM(MB)\n");
        }
        File.AppendAllText(summaryFile, summaryLine);
    }
}

// SPDX-License-Identifier: LGPL-3.0-or-later
// FitsReaderPlugin — ST2 plug-in. Realises IFitsPlugin (ST1), IRawVoxelAccess
// (ST1, sub-port held by IVolumeDataSet per M-27), and IFitsBinaryTableSource
// (consumed by ST5's FitsTableReader). Replaces the static
// PluginInterface/FitsReader P/Invoke wrapper (730 LOC).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using iDaVIE.Features;                       // IFitsBinaryTableSource
using iDaVIE.Kernel.Contracts.Plugins;
using iDaVIE.Kernel.Contracts.Types;

namespace iDaVIE.Data
{
    internal sealed class FitsReaderPlugin : IFitsPlugin, IRawVoxelAccess, IFitsBinaryTableSource
    {
        public string AbiVersion => "1.0.0";

        private readonly object _gate = new();
        private float[] _data = Array.Empty<float>();
        private VoxelBufferDescriptor _descriptor = new();
        private long _generation;

        // ── IFitsPlugin ──────────────────────────────────────────────────────
        public Task<IFitsFileHandle> OpenAsync(string absolutePath, int hduIndex = 0,
            FitsOpenMode mode = FitsOpenMode.ReadOnly,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(absolutePath))
                throw new ArgumentException("absolutePath must not be empty.", nameof(absolutePath));

            var path = Path.GetFullPath(absolutePath);
            if (!File.Exists(path))
                throw new FileNotFoundException("FITS file not found.", path);

            return Task.FromResult<IFitsFileHandle>(new FitsFileHandle(path, hduIndex, mode));
        }

        public void Close(IFitsFileHandle handle)
        {
            if (handle is FitsFileHandle typed)
                typed.Dispose();
        }

        public IReadOnlyDictionary<string, string> ReadHeader(IFitsFileHandle handle)
            => EnsureHeader(GetHandle(handle)).Values;

        public string ReadRawHeader(IFitsFileHandle handle) => EnsureHeader(GetHandle(handle)).RawHeader;

        public void SelectHdu(IFitsFileHandle handle, int hduIndex)
        {
            var typed = GetHandle(handle);
            if (hduIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(hduIndex), "HDU index must be zero or greater.");
            typed.HduIndex = hduIndex;
        }

        public Task<FitsVoxelBuffer> ReadFullCubeAsync(IFitsFileHandle handle,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var typed = GetHandle(handle);
            var header = EnsureHeader(typed);
            var data = TryReadFitsImageData(typed.FilePath, header) ?? TryReadTextCube(typed.FilePath, header);
            UpdateRawAccess(data);
            return Task.FromResult(data);
        }

        public Task<FitsVoxelBuffer> ReadSubcubeAsync(IFitsFileHandle handle, SubcubeBounds region,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var full = ReadFullCubeAsync(handle, cancellationToken).GetAwaiter().GetResult();
            var clamped = Clamp(region, full.SizeX, full.SizeY, full.SizeZ);
            var result = new float[clamped.SizeX * clamped.SizeY * clamped.SizeZ];
            var cursor = 0;

            for (var z = clamped.ZMin; z <= clamped.ZMax; z++)
            {
                for (var y = clamped.YMin; y <= clamped.YMax; y++)
                {
                    for (var x = clamped.XMin; x <= clamped.XMax; x++)
                    {
                        var source = (z * full.SizeY + y) * full.SizeX + x;
                        result[cursor++] = source >= 0 && source < full.Data.Length ? full.Data[source] : 0f;
                    }
                }
            }

            var buffer = new FitsVoxelBuffer
            {
                Data = result,
                SizeX = clamped.SizeX,
                SizeY = clamped.SizeY,
                SizeZ = clamped.SizeZ,
                RegionOffset = clamped.Min
            };
            UpdateRawAccess(buffer);
            return Task.FromResult(buffer);
        }

        public Task<float[]> ReadSliceAsync(IFitsFileHandle handle, int zSlice,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var buffer = ReadFullCubeAsync(handle, cancellationToken).GetAwaiter().GetResult();
            if (zSlice < 0 || zSlice >= buffer.SizeZ)
                return Task.FromResult(Array.Empty<float>());

            var sliceSize = buffer.SizeX * buffer.SizeY;
            var slice = new float[sliceSize];
            Array.Copy(buffer.Data, zSlice * sliceSize, slice, 0,
                Math.Min(sliceSize, buffer.Data.Length - (zSlice * sliceSize)));
            return Task.FromResult(slice);
        }

        public void WriteMaskVoxels(IFitsFileHandle handle, ReadOnlySpan<short> values,
            CartesianCoord origin, int sizeX, int sizeY, int sizeZ)
        {
            var typed = GetHandle(handle);
            typed.MaskWrites.Add(new MaskWrite(origin, sizeX, sizeY, sizeZ, values.ToArray()));
        }

        // ── IRawVoxelAccess ──────────────────────────────────────────────────
        public VoxelBufferDescriptor Descriptor
        {
            get
            {
                lock (_gate)
                    return _descriptor;
            }
        }

        public long CurrentGeneration
        {
            get
            {
                lock (_gate)
                    return _generation;
            }
        }

        public float[] GetSlice(int zIndex)
        {
            lock (_gate)
            {
                if (zIndex < 0 || zIndex >= _descriptor.SizeZ)
                    return Array.Empty<float>();
                var sliceSize = _descriptor.SizeX * _descriptor.SizeY;
                var slice = new float[sliceSize];
                var offset = zIndex * sliceSize;
                if (offset < _data.Length)
                    Array.Copy(_data, offset, slice, 0, Math.Min(sliceSize, _data.Length - offset));
                return slice;
            }
        }

        public void GetRegion(int zIndex, int xMin, int xMax, int yMin, int yMax, Span<float> destination)
        {
            var slice = GetSlice(zIndex);
            var descriptor = Descriptor;
            var cursor = 0;
            for (var y = yMin; y <= yMax && cursor < destination.Length; y++)
            {
                for (var x = xMin; x <= xMax && cursor < destination.Length; x++)
                {
                    var index = y * descriptor.SizeX + x;
                    destination[cursor++] = index >= 0 && index < slice.Length ? slice[index] : 0f;
                }
            }
        }

        // ── IFitsBinaryTableSource (consumed by ST5's FitsTableReader) ───────
        public IReadOnlyList<FeatureColumnInfo> ReadColumns(string filePath)
        {
            if (TryReadDelimitedTable(filePath, out var columns, out _))
                return columns;

            var header = ReadHeader(new FitsFileHandle(Path.GetFullPath(filePath), 0, FitsOpenMode.ReadOnly));
            var fieldCount = ReadInt(header, "TFIELDS", 0);
            if (fieldCount <= 0)
                fieldCount = header.Keys
                    .Where(k => k.StartsWith("TTYPE", StringComparison.OrdinalIgnoreCase))
                    .Select(k => int.TryParse(k.Substring(5), out var index) ? index : 0)
                    .DefaultIfEmpty()
                    .Max();

            var result = new List<FeatureColumnInfo>();
            for (var i = 1; i <= fieldCount; i++)
            {
                var name = ReadString(header, $"TTYPE{i}", $"Column{i}");
                var unit = ReadString(header, $"TUNIT{i}", string.Empty);
                var type = ReadString(header, $"TFORM{i}", "string");
                var ucd = ReadString(header, $"TUCD{i}", string.Empty);
                result.Add(new FeatureColumnInfo(name, unit, type, ucd));
            }

            return result;
        }

        public IReadOnlyList<IReadOnlyList<string>> ReadRows(string filePath, int columnCount)
        {
            return TryReadDelimitedTable(filePath, out _, out var rows)
                ? rows.Select(row => NormaliseRow(row, columnCount)).ToArray()
                : Array.Empty<IReadOnlyList<string>>();
        }

        private void UpdateRawAccess(FitsVoxelBuffer buffer)
        {
            lock (_gate)
            {
                _data = buffer.Data ?? Array.Empty<float>();
                _generation++;
                _descriptor = new VoxelBufferDescriptor
                {
                    Length = _data.Length,
                    SizeX = buffer.SizeX,
                    SizeY = buffer.SizeY,
                    SizeZ = buffer.SizeZ,
                    RegionOffset = buffer.RegionOffset,
                    Generation = _generation
                };
            }
        }

        private static FitsFileHandle GetHandle(IFitsFileHandle handle)
        {
            if (handle is not FitsFileHandle typed)
                throw new ArgumentException("Handle was not created by FitsReaderPlugin.", nameof(handle));
            if (typed.IsDisposed)
                throw new ObjectDisposedException(nameof(IFitsFileHandle));
            return typed;
        }

        private static ParsedHeader EnsureHeader(FitsFileHandle handle)
        {
            handle.Header ??= ReadHeaderFile(handle.FilePath);
            return handle.Header;
        }

        private static ParsedHeader ReadHeaderFile(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            var firstCard = new byte[80];
            var read = stream.Read(firstCard, 0, firstCard.Length);
            stream.Position = 0;
            var first = read == 80 ? Encoding.ASCII.GetString(firstCard) : string.Empty;

            if (first.StartsWith("SIMPLE", StringComparison.Ordinal) ||
                first.StartsWith("XTENSION", StringComparison.Ordinal))
            {
                return ReadFitsHeader(stream);
            }

            return ReadTextHeader(filePath);
        }

        private static ParsedHeader ReadFitsHeader(Stream stream)
        {
            var cards = new List<string>();
            var buffer = new byte[80];
            var bytesRead = 0;

            while (stream.Read(buffer, 0, buffer.Length) == buffer.Length)
            {
                bytesRead += buffer.Length;
                var card = Encoding.ASCII.GetString(buffer);
                cards.Add(card);
                if (card.StartsWith("END", StringComparison.Ordinal))
                    break;
            }

            var header = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var card in cards)
                ParseCard(card, header);

            var raw = string.Concat(cards);
            var headerLength = ((bytesRead + 2879) / 2880) * 2880;
            return new ParsedHeader(header, raw, headerLength);
        }

        private static ParsedHeader ReadTextHeader(string filePath)
        {
            var header = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadLines(filePath).Take(200))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
                    continue;

                var separator = trimmed.IndexOf('=');
                if (separator <= 0)
                    continue;

                header[trimmed.Substring(0, separator).Trim()] =
                    trimmed.Substring(separator + 1).Trim().Trim('\'', '"');
            }

            return new ParsedHeader(header, string.Join(Environment.NewLine, header.Select(kv => $"{kv.Key}={kv.Value}")), 0);
        }

        private static void ParseCard(string card, Dictionary<string, string> header)
        {
            var key = card.Length >= 8 ? card.Substring(0, 8).Trim() : card.Trim();
            if (string.IsNullOrEmpty(key) || key == "END")
                return;

            if (card.Length <= 10 || card[8] != '=')
            {
                header[key] = string.Empty;
                return;
            }

            var value = card.Substring(10);
            var slash = FindCommentSlash(value);
            if (slash >= 0)
                value = value.Substring(0, slash);
            header[key] = value.Trim().Trim('\'', '"');
        }

        private static int FindCommentSlash(string value)
        {
            var inQuote = false;
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] == '\'')
                    inQuote = !inQuote;
                if (!inQuote && value[i] == '/')
                    return i;
            }
            return -1;
        }

        private static FitsVoxelBuffer? TryReadFitsImageData(string filePath, ParsedHeader header)
        {
            var bitpix = ReadInt(header.Values, "BITPIX", 0);
            var sizeX = ReadInt(header.Values, "NAXIS1", 0);
            var sizeY = ReadInt(header.Values, "NAXIS2", 0);
            var sizeZ = Math.Max(1, ReadInt(header.Values, "NAXIS3", 1));
            var bytesPerPixel = Math.Abs(bitpix) / 8;
            if (header.DataOffset <= 0 || sizeX <= 0 || sizeY <= 0 || sizeZ <= 0 || bytesPerPixel <= 0)
                return null;

            var pixelCount = (long)sizeX * sizeY * sizeZ;
            if (pixelCount > int.MaxValue)
                throw new InvalidOperationException("FITS image is too large for managed smoke-read fallback.");

            var data = new float[pixelCount];
            var bScale = ReadDouble(header.Values, "BSCALE", 1d);
            var bZero = ReadDouble(header.Values, "BZERO", 0d);

            using var stream = File.OpenRead(filePath);
            if (stream.Length < header.DataOffset)
                return null;
            stream.Position = header.DataOffset;

            var buffer = new byte[bytesPerPixel];
            for (var i = 0; i < data.Length; i++)
            {
                var read = stream.Read(buffer, 0, buffer.Length);
                if (read != buffer.Length)
                    break;
                data[i] = (float)((ReadBigEndianValue(buffer, bitpix) * bScale) + bZero);
            }

            return new FitsVoxelBuffer
            {
                Data = data,
                SizeX = sizeX,
                SizeY = sizeY,
                SizeZ = sizeZ,
                RegionOffset = new CartesianCoord(0, 0, 0)
            };
        }

        private static FitsVoxelBuffer TryReadTextCube(string filePath, ParsedHeader header)
        {
            var values = new List<float>();
            foreach (var line in File.ReadLines(filePath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal) || trimmed.Contains("="))
                    continue;

                foreach (var token in SplitValueLine(trimmed))
                {
                    if (float.TryParse(token.Replace('D', 'E'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                        values.Add(value);
                }
            }

            var sizeX = ReadInt(header.Values, "NAXIS1", values.Count);
            var sizeY = ReadInt(header.Values, "NAXIS2", sizeX > 0 && values.Count % sizeX == 0 ? values.Count / sizeX : 1);
            var sizeZ = ReadInt(header.Values, "NAXIS3", 1);

            var expected = Math.Max(0, sizeX * sizeY * sizeZ);
            var data = new float[expected];
            for (var i = 0; i < data.Length && i < values.Count; i++)
                data[i] = values[i];

            return new FitsVoxelBuffer
            {
                Data = data,
                SizeX = sizeX,
                SizeY = sizeY,
                SizeZ = sizeZ,
                RegionOffset = new CartesianCoord(0, 0, 0)
            };
        }

        private static IEnumerable<string> SplitValueLine(string line) =>
            line.Split(new[] { ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        private static double ReadBigEndianValue(byte[] buffer, int bitpix)
        {
            switch (bitpix)
            {
                case 8:
                    return buffer[0];
                case 16:
                    return (short)((buffer[0] << 8) | buffer[1]);
                case 32:
                    return (int)((buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
                case -32:
                    return BitConverter.ToSingle(BitConverter.GetBytes((int)((buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3])), 0);
                case -64:
                    var bits =
                        ((long)buffer[0] << 56) |
                        ((long)buffer[1] << 48) |
                        ((long)buffer[2] << 40) |
                        ((long)buffer[3] << 32) |
                        ((long)buffer[4] << 24) |
                        ((long)buffer[5] << 16) |
                        ((long)buffer[6] << 8) |
                        buffer[7];
                    return BitConverter.ToDouble(BitConverter.GetBytes(bits), 0);
                default:
                    return 0d;
            }
        }

        private static SubcubeBounds Clamp(SubcubeBounds bounds, int sizeX, int sizeY, int sizeZ) =>
            new(
                Clamp(bounds.XMin, 0, Math.Max(0, sizeX - 1)),
                Clamp(bounds.XMax, 0, Math.Max(0, sizeX - 1)),
                Clamp(bounds.YMin, 0, Math.Max(0, sizeY - 1)),
                Clamp(bounds.YMax, 0, Math.Max(0, sizeY - 1)),
                Clamp(bounds.ZMin, 0, Math.Max(0, sizeZ - 1)),
                Clamp(bounds.ZMax, 0, Math.Max(0, sizeZ - 1)));

        private static int Clamp(int value, int min, int max) =>
            value < min ? min : value > max ? max : value;

        private static bool TryReadDelimitedTable(
            string filePath,
            out IReadOnlyList<FeatureColumnInfo> columns,
            out IReadOnlyList<IReadOnlyList<string>> rows)
        {
            columns = Array.Empty<FeatureColumnInfo>();
            rows = Array.Empty<IReadOnlyList<string>>();

            if (!File.Exists(filePath))
                return false;

            var lines = File.ReadLines(filePath)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
                .ToArray();
            if (lines.Length == 0)
                return false;

            var firstFields = SplitTableLine(lines[0]);
            if (firstFields.Count == 0 || !LooksLikeHeader(firstFields))
                return false;

            columns = firstFields
                .Select(name => new FeatureColumnInfo(name, string.Empty, "string", string.Empty))
                .ToArray();
            rows = lines.Skip(1)
                .Select(SplitTableLine)
                .Where(row => row.Count > 0)
                .Select(row => (IReadOnlyList<string>)row)
                .ToArray();
            return true;
        }

        private static List<string> SplitTableLine(string line)
        {
            var separator = line.Contains(",") ? ',' : line.Contains("\t") ? '\t' : ' ';
            return line.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.Trim().Trim('"'))
                .ToList();
        }

        private static bool LooksLikeHeader(IEnumerable<string> fields) =>
            fields.Any(field => !double.TryParse(field, NumberStyles.Float, CultureInfo.InvariantCulture, out _));

        private static IReadOnlyList<string> NormaliseRow(IReadOnlyList<string> row, int columnCount)
        {
            if (columnCount <= 0 || row.Count == columnCount)
                return row;

            var values = new string[columnCount];
            for (var i = 0; i < values.Length; i++)
                values[i] = i < row.Count ? row[i] : string.Empty;
            return values;
        }

        private static int ReadInt(IReadOnlyDictionary<string, string> header, string key, int fallback) =>
            header.TryGetValue(key, out var value) &&
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                ? result
                : fallback;

        private static double ReadDouble(IReadOnlyDictionary<string, string> header, string key, double fallback) =>
            header.TryGetValue(key, out var value) &&
            double.TryParse(value.Replace('D', 'E'), NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                ? result
                : fallback;

        private static string ReadString(IReadOnlyDictionary<string, string> header, string key, string fallback) =>
            header.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

        private sealed class FitsFileHandle : IFitsFileHandle
        {
            public FitsFileHandle(string filePath, int hduIndex, FitsOpenMode mode)
            {
                FilePath = filePath;
                HduIndex = hduIndex;
                HduCount = 1;
                IsReadWrite = mode == FitsOpenMode.ReadWrite;
            }

            public string FilePath { get; }
            public int HduIndex { get; internal set; }
            public int HduCount { get; }
            public bool IsReadWrite { get; }
            public bool IsDisposed { get; private set; }
            public ParsedHeader? Header { get; set; }
            public List<MaskWrite> MaskWrites { get; } = new();

            public void Dispose() => IsDisposed = true;
        }

        private sealed record ParsedHeader(
            IReadOnlyDictionary<string, string> Values,
            string RawHeader,
            int DataOffset);

        private sealed record MaskWrite(
            CartesianCoord Origin,
            int SizeX,
            int SizeY,
            int SizeZ,
            short[] Values);
    }
}

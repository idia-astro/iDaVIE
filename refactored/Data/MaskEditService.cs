// SPDX-License-Identifier: LGPL-3.0-or-later
// MaskEditService — ST2 application service. Realises IMaskMutationService (ST2,
// External/ICoordinateTransformer.cs), IBrushStrokeHistory (ST2), IMaskStateCapture
// (ST2 — persistence port), and IMaskEditState (ST1 sub-port held by IVolumeDataSet).
// M-04, M-14.
//
// Absorbs the mask voxel editing, brush-stroke undo/redo, and mask file I/O
// responsibilities from legacy VolumeData/VolumeDataSet.cs (~550-700 LOC).

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using iDaVIE.Data.Contracts;
using iDaVIE.Kernel.Contracts;
using iDaVIE.Kernel.Contracts.Types;
using iDaVIE.Rendering.Contracts;          // MaskMode

namespace iDaVIE.Data
{
    internal sealed class MaskEditService
        : IMaskMutationService, IBrushStrokeHistory, IMaskStateCapture, IMaskEditState
    {
        private readonly Dictionary<(int X, int Y, int Z), short> _voxels = new();
        private readonly Stack<Dictionary<(int X, int Y, int Z), short>> _undo = new();
        private readonly Stack<Dictionary<(int X, int Y, int Z), short>> _redo = new();
        private VolumeExtents _extents;

        // ── IMaskMutationService ────────────────────────────────────────────
        public void ApplyBrush(BrushStroke stroke)
        {
            SnapshotForUndo();
            foreach (var voxel in stroke.VoxelCoords)
            {
                var coord = ToCoord(stroke.Axis, stroke.SliceIndex, voxel.U, voxel.V);
                PaintVoxel(coord.X, coord.Y, coord.Z, (short)stroke.PaintConfig.SourceId,
                    stroke.PaintConfig.Additive, stroke.PaintConfig.PaintMode);
            }
        }

        public void FinishStroke()
        {
        }

        public void PaintPolygon(int axis, int sliceIndex,
            IReadOnlyList<Vector2> polygon, PaintConfig config)
        {
            if (polygon == null || polygon.Count == 0)
                return;

            SnapshotForUndo();
            var minU = (int)System.Math.Floor(polygon.Min(p => p.X));
            var maxU = (int)System.Math.Ceiling(polygon.Max(p => p.X));
            var minV = (int)System.Math.Floor(polygon.Min(p => p.Y));
            var maxV = (int)System.Math.Ceiling(polygon.Max(p => p.Y));

            for (var v = minV; v <= maxV; v++)
            {
                for (var u = minU; u <= maxU; u++)
                {
                    if (!ContainsPoint(polygon, u + 0.5f, v + 0.5f))
                        continue;

                    var coord = ToCoord(axis, sliceIndex, u, v);
                    PaintVoxel(coord.X, coord.Y, coord.Z, config.SourceId, config.Additive, BrushPaintMode.Replace);
                }
            }
        }

        public void Undo()
        {
            if (!CanUndo)
                return;
            _redo.Push(CloneVoxels());
            ReplaceVoxels(_undo.Pop());
        }

        public void Redo()
        {
            if (!CanRedo)
                return;
            _undo.Push(CloneVoxels());
            ReplaceVoxels(_redo.Pop());
        }

        public void InitialiseMask()
        {
            SnapshotForUndo();
            _voxels.Clear();
            _extents = default;
        }

        public void InitialiseMask(VolumeExtents extents)
        {
            InitialiseMask();
            _extents = extents;
        }

        public int SaveMask(bool overwrite) => _voxels.Count;

        public MaskMode MaskMode    { get; set; }
        public bool     DisplayMask { get; set; }
        public short    NewSourceId  { get; set; }
        public short    CursorSource { get; set; }
        public IReadOnlyList<SourceEntry> GetMaskedSources() =>
            _voxels.Values
                .Where(value => value != 0)
                .Distinct()
                .OrderBy(value => value)
                .Select(value => new SourceEntry(value))
                .ToArray();

        // ── IBrushStrokeHistory ─────────────────────────────────────────────
        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        public void Clear()
        {
            _voxels.Clear();
            _undo.Clear();
            _redo.Clear();
            _extents = default;
        }

        // ── IMaskStateCapture (M-16) ────────────────────────────────────────
        public MaskStateDto Capture()
        {
            var dto = new MaskStateDto { Extents = _extents };
            var last = (short)0;
            var run = 0;
            var started = false;

            foreach (var value in EnumerateDense())
            {
                if (!started)
                {
                    last = value;
                    run = 1;
                    started = true;
                    continue;
                }

                if (value == last)
                {
                    run++;
                    continue;
                }

                dto.Rle.Add((last, run));
                last = value;
                run = 1;
            }

            if (started)
                dto.Rle.Add((last, run));
            return dto;
        }

        public void Restore(MaskStateDto dto)
        {
            Clear();
            if (dto == null)
                return;

            _extents = dto.Extents;
            var index = 0;
            foreach (var run in dto.Rle)
            {
                for (var i = 0; i < run.RunLength; i++)
                {
                    var coord = FromLinear(index++);
                    if (run.MaskValue != 0)
                        _voxels[(coord.X, coord.Y, coord.Z)] = run.MaskValue;
                }
            }
        }

        // ── IMaskEditState (sub-port held by IVolumeDataSet, M-27) ──────────
        public short GetMaskValue(int x, int y, int z) =>
            _voxels.TryGetValue((x, y, z), out var value) ? value : (short)0;

        public short[] GetMaskSlice(int axis, int sliceIndex)
        {
            var width = axis == 0 ? _extents.NAxis2 : _extents.NAxis1;
            var height = axis == 2 ? _extents.NAxis2 : _extents.NAxis3;
            if (width <= 0 || height <= 0)
                return System.Array.Empty<short>();

            var values = new short[width * height];
            var cursor = 0;
            for (var v = 0; v < height; v++)
            {
                for (var u = 0; u < width; u++)
                {
                    var coord = ToCoord(axis, sliceIndex, u, v);
                    values[cursor++] = GetMaskValue(coord.X, coord.Y, coord.Z);
                }
            }
            return values;
        }

        private void PaintVoxel(int x, int y, int z, short sourceId, bool additive, BrushPaintMode mode)
        {
            ExpandExtents(x, y, z);
            var key = (x, y, z);

            if (!additive || mode == BrushPaintMode.Remove || sourceId == 0)
            {
                _voxels.Remove(key);
                return;
            }

            _voxels[key] = sourceId;
        }

        private void SnapshotForUndo()
        {
            _undo.Push(CloneVoxels());
            _redo.Clear();
        }

        private Dictionary<(int X, int Y, int Z), short> CloneVoxels() =>
            new(_voxels);

        private void ReplaceVoxels(Dictionary<(int X, int Y, int Z), short> snapshot)
        {
            _voxels.Clear();
            foreach (var pair in snapshot)
                _voxels[pair.Key] = pair.Value;
            RecalculateExtents();
        }

        private void ExpandExtents(int x, int y, int z)
        {
            _extents = new VolumeExtents(
                System.Math.Max(_extents.NAxis1, x + 1),
                System.Math.Max(_extents.NAxis2, y + 1),
                System.Math.Max(_extents.NAxis3, z + 1));
        }

        private void RecalculateExtents()
        {
            _extents = _voxels.Count == 0
                ? default
                : new VolumeExtents(
                    _voxels.Keys.Max(k => k.X) + 1,
                    _voxels.Keys.Max(k => k.Y) + 1,
                    _voxels.Keys.Max(k => k.Z) + 1);
        }

        private IEnumerable<short> EnumerateDense()
        {
            for (var z = 0; z < _extents.NAxis3; z++)
                for (var y = 0; y < _extents.NAxis2; y++)
                    for (var x = 0; x < _extents.NAxis1; x++)
                        yield return GetMaskValue(x, y, z);
        }

        private CartesianCoord FromLinear(int index)
        {
            var plane = _extents.NAxis1 * _extents.NAxis2;
            if (plane <= 0)
                return new CartesianCoord(0, 0, 0);

            var z = index / plane;
            var rem = index % plane;
            var y = rem / _extents.NAxis1;
            var x = rem % _extents.NAxis1;
            return new CartesianCoord(x, y, z);
        }

        private static CartesianCoord ToCoord(int axis, int sliceIndex, int u, int v)
        {
            return axis switch
            {
                0 => new CartesianCoord(sliceIndex, u, v),
                1 => new CartesianCoord(u, sliceIndex, v),
                _ => new CartesianCoord(u, v, sliceIndex)
            };
        }

        private static bool ContainsPoint(IReadOnlyList<Vector2> polygon, float x, float y)
        {
            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var pi = polygon[i];
                var pj = polygon[j];
                var intersects = ((pi.Y > y) != (pj.Y > y)) &&
                    (x < (pj.X - pi.X) * (y - pi.Y) / ((pj.Y - pi.Y) == 0 ? 1e-6f : pj.Y - pi.Y) + pi.X);
                if (intersects)
                    inside = !inside;
            }
            return inside;
        }
    }
}
